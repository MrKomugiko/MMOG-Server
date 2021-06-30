using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Linq;

namespace MMOG
{
    class Client
    {
        public static int dataBufferSize = 4096;

        public int id;
        public Player player;
        public TCP tcp;
        public UDP udp;

        public Client(int _clientId)
        {
            id = _clientId;
            tcp = new TCP(id);
            udp = new UDP(id);
        }

        public class TCP
        {
            public TcpClient socket;

            private readonly int id;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;

            public TCP(int _id)
            {
                id = _id;
            }

            public void Connect(TcpClient _socket)
            {
                socket = _socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                stream = socket.GetStream();

                receivedData = new Packet();
                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

                ServerSend.Welcome(id, "Welcome to the server!");

               
            }

            public void SendData(Packet _packet)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                    }
                }
                catch(Exception)
                {
                   Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] Error sending data to player {id} via TCP:");// {_ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult _result)
            {
                try
                {
                    int _byteLength = stream.EndRead(_result);
                    if (_byteLength <= 0)
                    {
                        Server.clients[id].Disconnect();
                        return;
                    }

                    byte[] _data = new byte[_byteLength];
                    Array.Copy(receiveBuffer, _data, _byteLength);

                    receivedData.Reset(HandleData(_data));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception)
                {
                   // Console.WriteLine($"Error receiving TCP data: {_ex}");
                    Server.clients[id].Disconnect();
                }
            }

            private bool HandleData(byte[] _data)
            {
                int _packetLength = 0;

                receivedData.SetBytes(_data);

                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        return true;
                    }
                }

                while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
                {
                    byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (Packet _packet = new Packet(_packetBytes))
                        {
                            int _packetId = _packet.ReadInt();
                            if(_packetId == 0) Console.WriteLine("packet widmo -> nr. 0 ??????");
                            Server.packetHandlers[_packetId](id, _packet);
                        }
                    });

                    _packetLength = 0;
                    if (receivedData.UnreadLength() >= 4)
                    {
                        _packetLength = receivedData.ReadInt();
                        if (_packetLength <= 0)
                        {
                            return true;
                        }
                    }
                }

                if (_packetLength <= 1)
                {
                    return true;
                }

                return false;
            }

            public void Disconnect() {
                if (socket == null) return;
                socket.Close();
                stream = null;
                receivedData = null;
                receiveBuffer = null;
                socket = null;
            }
        }
        public class UDP
        {
            public IPEndPoint endPoint;

            private int id;

            public UDP(int _id)
            {
                id = _id;
            }

            public void Connect(IPEndPoint _endPoint)
            {
                endPoint = _endPoint;
                //ServerSend.UDPTest(id);
            }

            public void SendData(Packet _packet)
            {
                Server.SendUDPData(endPoint, _packet);
            }

            public void HandleData(Packet _packetData)
            {
                int _packetLength = _packetData.ReadInt();
                byte[] _packetBytes = _packetData.ReadBytes(_packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();
                        Server.packetHandlers[_packetId](id, _packet);
                    }
                });
            }

            public void Disconnect() {
                endPoint = null;
            }
        }
    
        public void SendIntoGame(string _playerName) {
            player = new Player(id, _playerName);

            // spawn nowego gracza - lokalnego
            foreach(Client _client in Server.clients.Values.Where(client=>client.player != null)) {
                if(_client.id != id) {
                   // Console.WriteLine($"Spawn Gracza [{_client.player.username}]");
                    ServerSend.SpawnPlayer(id, _client.player);
                }
            }

            // wysłanie info do wszystkich pozostałych graczy, że pojawił się nowy
            foreach (Client _client in Server.clients.Values.Where(client => client.player != null)) {
                ServerSend.SpawnPlayer(_client.id, player);
            }
        }
        public void SendIntoGame(Player registeredPlayerData) {
            // przypisanie aktualnego gracza
            player = registeredPlayerData;
            Console.WriteLine("sendintogame "+ player.Username );

           // spawn nowego gracza - lokalnego
            foreach(Client _client in Server.clients.Values.Where(client=>client.player != null)) {
                if(_client.id != id) 
                {
                   // Console.WriteLine($"Spawn Gracza [{_client.player.username}]");
                   Console.WriteLine("1. spawnplayer : id="+player.Id+" client.player = " +_client.player.Username);
                    ServerSend.SpawnPlayer(player.Id, _client.player);
                }
            }

            // wysłanie info do wszystkich pozostałych graczy, że pojawił się nowy
            foreach (Client _client in Server.clients.Values.Where(client => client.player != null)) {

                Console.WriteLine("2. spawnplayer : id="+_client.id+" client.player = " +player.Username);
                ServerSend.SpawnPlayer(_client.id, player);
            }
        }
        public void Disconnect() {
           
            if(player != null) {
                if (tcp.socket == null) return;
                ServerHandle.PlayersMoveInputRequests[player.Id] =0;

                Console.WriteLine($"[{tcp.socket.Client.RemoteEndPoint}][{player.Username}] has disconnected.");


                DungeonLobby room = Server.dungeonLobbyRooms.Where(room=>room.Players.Contains(player)).FirstOrDefault();
                
                if(room != null)
                {
                    if(room.LobbyOwner == player)
                    {
                        DungeonLobby.RemoveExistingLobby(_room: room);
                    }
                    Server.dungeonLobbyRooms.Where(room=>room.Players.Contains(player)).First().Players.Remove(player);
                }
            
                // anulowanie wykonywanych akcji przed wylogowaniem
                ServerHandle.ClearAllExecutingPlayerAction(player.Id);

                ServerSend.UpdateChat($"[{player.Username}] has disconnected.");

                ServerSend.RemoveOfflinePlayer(player.Id);
            }

            player = null;

            tcp.Disconnect();
            udp.Disconnect();
   
        }

    }
}
