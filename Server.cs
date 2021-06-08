using System.Reflection.Metadata;
using System.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace MMOG
{
    class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers;

        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        public static Dictionary<int, string> listaObecnosci = new Dictionary<int, string>();
        public static Dictionary<Vector3, string> GLOBAL_MAPDATA = new Dictionary<Vector3, string>();
        public static Dictionary<Vector3, string> GROUND_MAPDATA = new Dictionary<Vector3, string>();
        public static int UpdateVersion = 1001;
        public static void Start(int _maxPlayers, int _port)
        {
            MaxPlayers = _maxPlayers;
            Port = _port;

            Console.WriteLine("Starting server...");
            InitializeServerData();

            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);
 
            int SIO_UDP_CONNRESET = -1744830452;
            udpListener = new UdpClient(Port);
            udpListener.Client.IOControl((IOControlCode) SIO_UDP_CONNRESET,new byte[] { 0, 0, 0, 0 },null);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            Console.WriteLine($"Server started on port {Port}.");

            
            ServerHandle.LoadMapDataFromFile(MAPTYPE.OBSTACLEMAP,ref Server.GLOBAL_MAPDATA, Constants.MAP_DATA_FILE_PATH);
            Console.WriteLine("GLOBAL size new: " + GLOBAL_MAPDATA.Count);

           
            ServerHandle.LoadMapDataFromFile(MAPTYPE.GROUND_MAP,ref Server.GROUND_MAPDATA, Constants.GROUND_MAP_DATA_FILE_PATH);
            Console.WriteLine("GROUND size new: " + GROUND_MAPDATA.Count);

        }

        private static void TCPConnectCallback(IAsyncResult _result)
        {
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);

            for (int i = 1; i <= MaxPlayers; i++)
            {
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(_client);
                   // Console.WriteLine($"Incoming connection from {_client.Client.RemoteEndPoint}...");
                    return;
                }
            }

            Console.WriteLine($"{_client.Client.RemoteEndPoint} failed to connect: Server full!");
        }

        private static void UDPReceiveCallback(IAsyncResult _result)
        {
            try
            {
                IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
                try { 
                    udpListener.BeginReceive(UDPReceiveCallback, null);
                } 
                catch {
                    Console.WriteLine("error udp - 0");
                        return; 
                }

                if (_data.Length < 4)
                {
                    return;
                }

                using (Packet _packet = new Packet(_data))
                {
                    int _clientId = _packet.ReadInt();

                    if (_clientId == 0)
                    {
                        return;
                    }

                    if (clients[_clientId].udp.endPoint == null)
                    {
                        clients[_clientId].udp.Connect(_clientEndPoint);
                        return;
                    }

                    if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                    {
                        clients[_clientId].udp.HandleData(_packet);
                    }
                }
            }
            catch (Exception )
            {
               // Console.WriteLine($"Error receiving UDP data: {_ex}");
                Console.WriteLine("error udp - 1");
               
              //  wudpListener.BeginReceive(UDPReceiveCallback, null);
               // return;
               
            }
        }

        public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
        {
            try
            {
                if (_clientEndPoint != null)
                {
                    udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                }
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
            }
        }

        private static void InitializeServerData()
        {
            for (int i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i, new Client(i));
            }

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
               // { (int)ClientPackets.updTestReceived, ServerHandle.UDPTestReceived },
                { (int)ClientPackets.playerMovement, ServerHandle.PlayerMovement },
                { (int)ClientPackets.SendChatMessage, ServerHandle.SendChatMessage },
                { (int)ClientPackets.PingReceived, ServerHandle.PingReceived },
                { (int)ClientPackets.SEND_MAPDATA_TO_SERVER, ServerHandle.MapDataReceived },
                { (int)ClientPackets.downloadLatestMapUpdate, ServerHandle.SendLatestUpdateMapDataToClient },
                { (int)ClientPackets.download_recentMapVersion, ServerHandle.SendNumberOfLAtestMapUpdate },
                { (int)ClientPackets.clientChangeLocalisation, ServerHandle.ChangePlayerLocalisation }


            };

            Console.WriteLine("Initialized packets.");
        }

        public static void ZaktualizujListeObecnosci(int afkId) {
            listaObecnosci.Remove(afkId);
        }
    }
}
