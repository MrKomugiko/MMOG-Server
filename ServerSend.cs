using MMOG;
using System;
using System.Collections.Generic;
using System.Numerics;
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
                    try
                    {
                        Server.clients[i].tcp.SendData(_packet);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("exception SendTCPDataTOALL: "+ex.Message);
                    }
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
            Console.WriteLine("send packet Hello");
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
                _packet.Write((int)_player.CurrentLocation); // iint
             
                SendTCPData(_toClient, _packet);
            }
        }
        public static void PlayerPosition(Player _player) {
            // odblokowanie ruchu gracza
            // ServerHandle.PlayersMoveInputRequests[_player.id] = 0;
            //Console.WriteLine($"[{_player.username}] wykonał ruch.");
            using (Packet _packet = new Packet((int)ServerPackets.playerPosition)) {
                _packet.Write(_player.id);
                _packet.Write(_player.position);

                SendTCPDataToAll(_packet);
            }
            ServerHandle.PlayersMoveExecuted[_player.id]++;
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
        public static void DownloadMapData(int fromID, MAPTYPE mapType, LOCATIONS mapLocation) {
            Console.WriteLine("Wysłanie żądania o dostarczenie danych mapy przez klienta-admina");
            using (Packet _packet = new Packet((int)ServerPackets.downloadMapData)) {
                _packet.Write((int)mapType);
                _packet.Write((int)mapLocation);
                SendTCPData(fromID,_packet);
            }
        }

        // wyslanie info o aktualnej wesji update`a
        public static void SendCurrentUpdateVersionNumber(int sendToID = -1) {
            using (Packet _packet = new Packet((int)ServerPackets.sendCurrentUpdateNumber)) {
            // TODO: rozdzielenie updateów map dla kazdej z oobno
                _packet.Write(UpdateChecker.serverJsonFile);

                if(sendToID == -1) {
                    Console.WriteLine("wysłano JSON z numerami wersji DO WSZYSTKICH");
                    SendTCPDataToAll(_packet); // wysłanie pakietu do wszystkich
                    return;
                }
                
                Console.WriteLine("wysłano JSON z numerami wersji do gracza #" + sendToID);
                SendTCPData(sendToID,_packet); // wysłanie pakietu do konkretnej osoby 
            }
        }
       // TODO:
        //public static void SendInformationAboutDataVersion(int _toClient, DATATYPE? _data = null, LOCATIONS? _location = null, MAPTYPE? _maptype = null, ITEMS? _items = null) {
        //  //  SEND ALL IN ONE BIG PACKET
        //    if (_data == null && _location == null && _maptype == null) {
        //        var datatypeCount = Enum.GetNames(typeof(DATATYPE)).Length;

        //        switch(_data) {
            
        //            case DATATYPE.Locations:
        //                var LocationCount = Enum.GetNames(typeof(LOCATIONS)).Length;
        //                var mapTypesCount = Enum.GetNames(typeof(MAPTYPE)).Length;

        //                int packetSize = 0;
        //                 for (int location = 0; location < LocationCount; location++) {
        //                    for (int maptype = 0; maptype < mapTypesCount; maptype++) {
        //                        packetSize++;
        //                        }
                            
        //                }
        //                using (Packet _packet = new Packet((int)ServerPackets.InformationAboutDataVersion)) {
        //                    _packet.Write(packetSize);      // numer pakietu
        //                        int datatype = (int)_data;
        //                        for (int location = 0; location < LocationCount; location++) {
        //                            for (int maptype = 0; maptype < mapTypesCount; maptype++) {

        //                                _packet.Write(datatype);        // DATATYPE
        //                                _packet.Write(location);        // Locations
        //                                _packet.Write(maptype);         // MAPTYPE
        //                                _packet.Write(UpdateChecker.GetVersionOf(_datatype:(DATATYPE)datatype,_location:(LOCATIONS)location, _maptype:(MAPTYPE)maptype));
        //                            }
        //                        }
                            

        //                    SendTCPData(_toClient, _packet); // wysłanie pakietu do konkretnej osoby 
        //                }
        //                break;

        //        }
        //    }

        //   // SEND SPECIFIC DATA

        //}

        public static void SendMapDataToClient(int id, LOCATIONS _location, MAPTYPE _mapType, Dictionary<Vector3, string> REFERENCEMAP) {
          try{

          
            if(REFERENCEMAP.Count == 0) {
                Console.WriteLine("Brak danych mapy na serwerze, wysyłanie przerwane.");
            }

            using (Packet _packet = new Packet((int)ServerPackets.SEND_MAPDATA_TO_CLIENT)) 
            {
                _packet.Write(UpdateChecker.GetVersionOf(_location,_mapType));          // aktualna wersja mapy
                _packet.Write((int)_location);                                          // int lokalizacja
                _packet.Write((int)_mapType);                                           // INT rodzaj mapy
                _packet.Write(REFERENCEMAP.Count);                                      // dodanie wielkości przesyłanego pakietu
                Console.WriteLine("wielokosc paczki: "+REFERENCEMAP.Count);

                foreach(var kvp in REFERENCEMAP) {
                    _packet.Write(kvp.Key);                                             // dodanie Vector3
                    _packet.Write(kvp.Value);                                           // dodanie string = wartosci pola = nazwy
                }   

                SendTCPData(id, _packet);
            }

            Console.WriteLine("Pomyslnie wysłano dane do klienta");
          }
          catch(Exception ex) { Console.WriteLine(ex.Message);}
        }
        #endregion
    }
        }
