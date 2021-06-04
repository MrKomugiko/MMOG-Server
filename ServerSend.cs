using System;
using System.Collections.Generic;
using System.Text;

namespace MMOG
{
    class ServerSend
    {
        private static void SendTCPData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();
            Server.clients[_toClient].tcp.SendData(_packet);
        }
        private static void SendUDPData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();
            Server.clients[_toClient].udp.SendData(_packet);
        }
        private static void SendTCPDataToAll(Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                try {
                    Server.clients[i].tcp.SendData(_packet);
                }
                catch(KeyNotFoundException ex){
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (i != _exceptClient)
                {
                    Server.clients[i].tcp.SendData(_packet);
                }
            }
        }

        private static void SendUDPDataToAll(Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                Server.clients[i].udp.SendData(_packet);
            }
        }
        private static void SendUDPDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (i != _exceptClient)
                {
                    Server.clients[i].udp.SendData(_packet);
                }
            }
        }

        #region Packets
        public static void Welcome(int _toClient, string _msg)
        {
            using (Packet _packet = new Packet((int)ServerPackets.welcome))
            {
                _packet.Write(_msg);
                _packet.Write(_toClient);

                SendTCPData(_toClient, _packet);
            }
        }
        public static void UDPTest(int _toClient)
        {
          /*  using (Packet _packet = new Packet((int)ServerPackets.udpTest))
            {
                _packet.Write("A test packet for UDP.");

                SendUDPData(_toClient, _packet);
            }*/
        }
        public static void SpawnPlayer(int _toClient, Player _player) {
            using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer)) {
                _packet.Write(_player.id);
                _packet.Write(_player.username);
                _packet.Write(_player.position);
                _packet.Write(_player.rotation);
             
                SendTCPData(_toClient, _packet);
            }
        }
        public static void PlayerPosition(Player _player) {
            //Console.WriteLine($"[{_player.username}] wykonał ruch.");
            using (Packet _packet = new Packet((int)ServerPackets.playerPosition)) {
                _packet.Write(_player.id);
                _packet.Write(_player.position);

                SendUDPDataToAll(_packet);
            }
        }
        public static void UpdateChat(string _msg) {
            using (Packet _packet = new Packet((int)ServerPackets.updateChat)) {
                _packet.Write(_msg);

                SendTCPDataToAll(_packet);
            }
        }

        public static void Ping_ALL() {
            Server.listaObecnosci.Clear();
            foreach(var player in Server.clients.Values) {
                if (player.player == null) continue;

                // sprawdzanie czy na liscie obecnosci znajduje sie juz ten gracz nie nadpisuj go, 
                if (Server.listaObecnosci.ContainsKey(player.id)) continue;

                // jezeli nowy gracz dolaczył, dopisz go do listy
                Server.listaObecnosci.Add(player.id, $"[....] \t[#{player.id} {player.player.username}]");
            };

            using (Packet _packet = new Packet((int)ServerPackets.ping_ALL)) {
                _packet.Write(1);
                SendTCPDataToAll(_packet);
            }
        }

        public static void UpdateChat_NewUserPost(int _fromPlayer, string _msg)
        {
            using (Packet _packet = new Packet((int)ServerPackets.updateChat_NewUserPost)) {
                _packet.Write(_fromPlayer);
                _packet.Write(_msg);
                SendTCPDataToAll(_packet);
            }
        }

        public static void RemoveOfflinePlayer(int _playerToRemove) {
            using (Packet _packet = new Packet((int)ServerPackets.removeOfflinePlayer)) {
                _packet.Write(_playerToRemove);
                SendTCPDataToAll(_packet);
            }
        }

        public static void DownloadMapData(int fromID) {
            Console.WriteLine("Wysłanie żądania o dostarczenie danych mapy");
            using (Packet _packet = new Packet((int)ServerPackets.downloadMapData)) {
                _packet.Write(1);
                SendTCPData(fromID,_packet);
            }
        }

        // wyslanie info o aktualnej wesji update`a
        public static void SendCurrentUpdateVersionNumber(int sendToID = -1) {
            
            using (Packet _packet = new Packet((int)ServerPackets.sendCurrentUpdateNumber)) {


                _packet.Write(Server.UpdateVersion);

                if(sendToID == -1) {
                    Console.WriteLine("wysłano numer wersji DO WSZYSTKICH");
                    SendTCPDataToAll(_packet); // wysłanie pakietu do wszystkich
                    return;
                }
                
                Console.WriteLine("wysłano numer wersji do gracza #"+sendToID);
                SendTCPData(sendToID,_packet); // wysłanie pakietu do konkretnej osoby 
            }
        }

        public static void SendMapDataToClient(int id) {
            Console.WriteLine("Wysłanie wszystkich danych mapy do gracza #"+id);
            using (Packet _packet = new Packet((int)ServerPackets.SEND_MAPDATA_TO_CLIENT)) 
            {
                _packet.Write(Server.MAPDATA.Count); // dodanie wielkości przesyłanego pakietu
                foreach(var kvp in Server.MAPDATA) {
                    _packet.Write(kvp.Key); // dodanie Vector3
                    _packet.Write(kvp.Value); // dodanie string = wartosci pola = nazwy
                }   

            SendTCPData(id, _packet);
            }

            Console.WriteLine("Pomyslnie wysłano dane do klienta");
        }
        #endregion
    }
        }
