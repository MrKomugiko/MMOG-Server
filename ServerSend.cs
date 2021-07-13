using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Text.Json;

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
                    Console.WriteLine("Send tcp to all "+ex.Message);
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
                        Console.WriteLine("exception SendTCPDataTOALL except one: "+ex.Message);
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

        internal static void LoginResponse( int _toClient)
        {
            using (Packet _packet = new Packet((int)ServerPackets.LoginResponse))
            {
                _packet.Write("Podane dane są nieprawidłowe");
                SendTCPData(_toClient, _packet);
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

        internal static void ConfirmAccountCreation(string _confirmationCode, int _toClient)
        {
            using (Packet _packet = new Packet((int)ServerPackets.RegistrationResponse))
            {
                _packet.Write(_confirmationCode);
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
                _packet.Write(_player.Id);
                _packet.Write(_player.Username);
                _packet.Write(_player.Position);
                _packet.Write(_player.Rotation);
                _packet.Write((int)_player.CurrentLocation); // iint
                _packet.Write(_player.CurrentFloor);
                _packet.Write(_player.dungeonRoom); // current dungeon room id

             
                SendTCPData(_toClient, _packet);
            }
        }

        internal static void KickedFromDungeonRoom(int  _toClient, DungeonLobby _dungeonlobby)
        {
             using (Packet _packet = new Packet((int)ServerPackets.kickFromDungeonRoom))
              {
                // bez szczegolow, generalnie info zeby wyjsc
                _packet.Write(_dungeonlobby.LobbyID); // int -> dungeon ID

                SendTCPData(_toClient, _packet);
            }
        }
    public static void RunExitDungeonCounter(int _toClient, DungeonLobby _dungeonlobby)
        {
            Console.WriteLine("tworzenie pakietu RunCounter");
            using (Packet _packet = new Packet((int)ServerPackets.RunCounter))
            {
                _packet.Write(_dungeonlobby.LobbyID);           
                Console.WriteLine("wyslanie pakietu do klienta o ID = "+ _toClient);
                SendTCPData(_toClient, _packet);  
            }
        }
        

        public static void SendCurrentUpdatedDungeonLobbyData(int? _toClient = null, DungeonLobby.DUNGEONS dungeon = default, string _action = "null", int _roomID = 0)
        {
            if(Server.dungeonLobbyRooms == null) Server.dungeonLobbyRooms = new List<DungeonLobby>();
            var data = Server.dungeonLobbyRooms;

           // Console.WriteLine("send dungeon list-lobby update");
            
            using (Packet _packet = new Packet((int)ServerPackets.CurrentDungeonRoomsStatus))
              {
                _packet.Write(_action);                                                                                      
                Console.WriteLine("_action: "+_action);                
                
                if(dungeon != default){

                 //   Console.WriteLine("zdefiniowano rodzaj lobby-dungeonu = "+dungeon.ToString());
                    // jezeli jest jakis wpis
                    if(data.Count>0)
                    {
                        data.Where(dungeon=>dungeon.DungeonLocation.ToString() == dungeon.ToString());
                    }
                    else
                    {
                        Console.WriteLine("brak danych");
                    }
                }
                
                _packet.Write(data.Count);
             //   Console.WriteLine("Count: "+data.Count);
              //  Console.WriteLine("Count of rooms: "+data.Where(room=>room != null).Count());


                if(data.Count>0)
                {
                    foreach(var room in data)
                    {
                        if(room == null) continue;
                        
                        _packet.Write(room.LobbyID);                    Console.WriteLine(" *LobbyID: "+room.LobbyID);
                        _packet.Write(room.LobbyOwner.Username);        //Console.WriteLine(" *LobbyOwner.Username: "+room.LobbyOwner.Username);
                        _packet.Write((int)room.DungeonLocation);       //Console.WriteLine(" *DungeonLocation: "+room.DungeonLocation);
                        _packet.Write(room.MaxPlayersCapacity);         //Console.WriteLine(" *MaxPlayersCapacity: "+room.MaxPlayersCapacity);
                        _packet.Write(room.IsStarted);                  Console.WriteLine(" *IsStarted: "+room.IsStarted);
                        _packet.Write(room.PlayersCount);               //Console.WriteLine(" *PlayersCount: "+room.PlayersCount);
                        foreach(var player in room.Players)
                        {
                            _packet.Write(player.Username);             //Console.WriteLine(" \t - player: "+player.Username);
                        }
                    }

                    if(_toClient != null) 
                     // SendTCPData((int)_toClient,_packet);
                     // TODO: zmienic odbiorcow, i wysylac tylko osoba zainteresowanym xD 
                      SendTCPDataToAll(_packet);

                    else 
                      SendTCPDataToAll(_packet);
                }
            }
        }

        internal static void RemoveDeletedRoom(DungeonLobby.DUNGEONS dungeon, int roomId, DungeonLobby _dungeonLobby)
        {
            using (Packet _packet = new Packet((int)ServerPackets.removeLobbyRoom))
            {
                //Console.WriteLine("wyslanie info czyczczace nieistniejacy juz pokoj");
                _packet.Write((int)dungeon);
                _packet.Write(roomId);

                //SendTCPDataToAll(_exceptClient:_dungeonLobby.LobbyOwner.Id, _packet);
                SendTCPDataToAll(_packet);
            }
        }

        public static void PlayerPositionToALL(Player _player, bool _isTeleported = false) {
            ;
            using (Packet _packet = new Packet((int)ServerPackets.playerPosition)) {
                _packet.Write(_isTeleported);
                _packet.Write(_player.Id);
                _packet.Write(_player.Position);

                SendTCPDataToAll(_packet);
            }
            ServerHandle.PlayersMoveInputRequests[_player.Id]=0;
        }      

         public static void PlayerPositionToGroup(List<Player> _group, Player _player, bool _isTeleported = false) {
            
            foreach(var player in _group)
            {
                using (Packet _packet = new Packet((int)ServerPackets.playerPosition)) {
                    _packet.Write(_isTeleported);
                    _packet.Write(_player.Id);
                    _packet.Write(_player.Position);

                    SendTCPData(player.Id,_packet);

                    Console.WriteLine($"wyslano info o przeteleportowaniu do {player.Username}");
                }
            }
            ServerHandle.PlayersMoveInputRequests[_player.Id]=0;
        }   

        public static void UpdateChat(string _msg) {
            using (Packet _packet = new Packet((int)ServerPackets.updateChat)) {
                _packet.Write(_msg);

                SendTCPDataToAll(_packet);
            }
        }

        internal static void CollectItem(int ID, Item item)
        {
            using (Packet _packet = new Packet((int)ServerPackets.colectItem)) 
            {
                _packet.Write(ID); // INT server current player ID
                 // przekazywanie danych itemkka
                _packet.Write(item.id);
                _packet.Write(item.name);
                _packet.Write(item.value);
                _packet.Write(item.level);
                _packet.Write(item.stackable);
                _packet.Write(item.stackSize);
                _packet.Write(item.description);

                SendTCPData(ID,_packet);
            }
        }

        internal static void RemoveItemFromMap(LOCATIONS currentLocation, MAPTYPE obstacle_MAP, Vector3 position)
        {
           using (Packet _packet = new Packet((int)ServerPackets.removeItemFromMap)) {
                _packet.Write((int)currentLocation);
                _packet.Write((int)obstacle_MAP);
                _packet.Write(position);

                SendTCPDataToAll(_packet);
            }
        }

        public static void Ping_ALL() {
            //nie usuwanie Serverowych kont
            Server.listaObecnosci.Clear();
            foreach(var player in Server.clients.Values) {
                if (player.player == null) continue;

                // sprawdzanie czy na liscie obecnosci znajduje sie juz ten gracz nie nadpisuj go, 
                if (Server.listaObecnosci.ContainsKey(player.id)) continue;


                // jezeli nowy gracz dolaczył, dopisz go do listy
                Server.listaObecnosci.Add(player.id, $"[....] \t[#{player.id} {player.player.Username}]");

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
        
             string serverJsonFile = (JsonSerializer.Serialize(UpdateChecker.SERVER_UPDATE_VERSIONS)).ToString();
                _packet.Write(serverJsonFile);

                if(sendToID == -1) {
                 //   Console.WriteLine("wysłano JSON z numerami wersji DO WSZYSTKICH");
                    SendTCPDataToAll(_packet); // wysłanie pakietu do wszystkich
                    return;
                }
                
                //Console.WriteLine("wysłano JSON z numerami wersji do gracza #" + sendToID);
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
          catch(Exception ex) { Console.WriteLine(" send mapdata to client "+ex.Message);}
        }
        #endregion
    }
        }
