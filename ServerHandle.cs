using System.Net.Security;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MMOG
{
    partial class ServerHandle
    {
        public static void WelcomeReceived(int _fromClient, Packet _packet) {
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();

            Console.WriteLine($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
            if (_fromClient != _clientIdCheck) {
                Console.WriteLine($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
            }
            Server.clients[_fromClient].SendIntoGame(_username);


  
        }
        public static void LoginDataReceived(int _fromClient, Packet _packet) {
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();
            string _password = _packet.ReadString();
            string _MODE = _packet.ReadString();
            if(_MODE == "LOGIN")
            {
                // TODO: ogarnac to jak anlezy xd narazie napisane z reki
                SimplePlayerCreditionals playerdata = Server.USERSDATABASE.Where(user=>user.Username == _username && user.Password == _password).FirstOrDefault();
                if(playerdata != null) 
                {
                    try
                    {                                
                        // obsługa multikonta na te same dane
                        foreach(var users in Server.clients.Values)
                        {
                            // sprawdzanie czy UserId jest juz zalogowany
                            int alreadyLoggedInPlayer_UserId = Server.GetUserId(_username);
                            if(users.player !=  null) // jezeli taki gracz jest aktualnie dostepnmy
                            {
                                    if(users.player.UserID == alreadyLoggedInPlayer_UserId)
                                    {
                                        //sprawdzanie Serverowego ID juz zalogowanego osobnika
                                        // i go wyjebanie 'd
                                        Server.clients[Server.GetPlayerByUserID(alreadyLoggedInPlayer_UserId).Id].Disconnect();
                                    }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("prawdopodonie wywali bład poczas logowania gdy na serwerze sa juz boty, "+ ex.Message);
                    }

                    Console.WriteLine($"ktos id:({_username}) chce sie zalogowac na swoje konto");
                    Console.WriteLine("ACCES GRANTED");
                    Server.clients[_fromClient].SendIntoGame(Server.GetPlayerData(playerdata.UserID, serverID:_fromClient));
                }
                else
                {
                    Console.WriteLine("ACCES DENIED");
                    ServerSend.LoginResponse(_fromClient);
                  
                }
            }
            if(_MODE == "REGISTER")
            {
                //sprawdzenie czy konto juz istnieje
                if(Server.Players_DATABASE.Where(player=>player.Username == _username).Any())
                {
                    ServerSend.ConfirmAccountCreation(_confirmationCode:"FAILED", _toClient: _fromClient);
                    return;
                }
                foreach(var players in Server.Players_DATABASE)
                {
                    Console.WriteLine("chyba puste: "+players.Username);
                }
                Console.WriteLine($"Nowy towarzysz właśnie utworzył konto: {_username}:{_password}");

                Server.Players_DATABASE.Add(new Player(_fromClient,_username));

                Server.USERSDATABASE.Add(new SimplePlayerCreditionals(userID:Server.Players_DATABASE.Where(player=>player.Username == _username).FirstOrDefault().UserID,_username,_password));
                ServerSend.ConfirmAccountCreation(_confirmationCode:"SUCCES", _toClient: _fromClient);
            }
        }


        public static void UDPTestReceived(int _fromClient, Packet _packet) {
            string _msg = _packet.ReadString();

            Console.WriteLine($"Received packet via UDP. Contains message: {_msg}");
        }

        public static int[] PlayersMoveInputRequests = new int[50];
        
        public static void PlayerMovement(int _fromClient, Packet _packet) 
        {
            if(PlayersMoveInputRequests[_fromClient] > 0)
            {
               // Console.WriteLine("nie tak szybko koleeszko");
                return;
            }
            
          //  Console.WriteLine("start process moving command"+PlayersMoveInputRequests[_fromClient]+" / "+PlayersMoveExecuted[_fromClient]);
            PlayersMoveInputRequests[_fromClient]= 1;
            bool[] _inputs = new bool[_packet.ReadInt()]; // pobieranie wielości tablicy
            for (int i = 0; i < _inputs.Length; i++) {
                _inputs[i] = _packet.ReadBool(); // pobieranie kolejnych wartości bool
            }
            Quaternion _rotation = _packet.ReadQuaternion(); // pobieranie rotacji

            Server.clients[_fromClient].player.SetInput(_inputs, _rotation); // przesłanie informacji dot. wcisnietych input klawiszy danego clienta                                                                    

        }

        public static void SendChatMessage(int _fromClient, Packet _packet) {
            //tutaj obieram wiadomość od klienta i rozsyłam ją do wszystkich aby ją zaktualizowali na czacie
            try
            {
                int _id = _packet.ReadInt();
                string _msg = _packet.ReadString();
                Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}]:[{Server.clients[_id].player.Username}]:{_msg}");

                ServerSend.UpdateChat_NewUserPost(_id, _msg);
            }
            catch(Exception ex)
            {
               Console.WriteLine("bład wysylania wiadomosci na czacie :"+ex.Message);
            }
        }


        public static void PingReceived(int _fromClient, Packet _packet) {
          try {

              if(Server.clients[_fromClient].player != null)
              {
                    Server.listaObecnosci[_fromClient] = $"[.OK.] \t[#{_fromClient} {Server.clients[_fromClient].player.Username}]";
              }
            } 
            catch(Exception ex) {
               Console.WriteLine("Cos poszło nie tak z aktualizacją statusu obecności gracza" + ex.Message);
           }
        }

        public static void MapDataReceived(int _fromClient, Packet _packet) {
            Dictionary<Vector3, string> tempDict = new Dictionary<Vector3, string>();
            int _dataSize = _packet.ReadInt();
            DATATYPE _datatype = (DATATYPE)_packet.ReadInt();
            LOCATIONS _location = (LOCATIONS)_packet.ReadInt();
            MAPTYPE _mapType = (MAPTYPE)_packet.ReadInt();

            Console.WriteLine($"Otrzymano: {_dataSize} / {_location} / {_mapType}");
            for (int i = 0; i < _dataSize; i++) 
            {
                var key = _packet.ReadVector3();
                string value = _packet.ReadString();
                tempDict.Add(key, value);
            }

            //switch (_mapType)
            //{
            //    case MAPTYPE.Ground_MAP:
            //        Console.WriteLine("Odebrano dane mapy typu GROUND.");
            //        path = Constants.GROUND_MAP_DATA_FILE_PATH;
            //    break;

            //    case MAPTYPE.Obstacle_MAP:
            //        Console.WriteLine("Odebrano dane mapy typu GLOBAL/OBSTACLE.");
            //        path = Constants.MAP_DATA_FILE_PATH;
            //    break;
            //}
            string path = Constants.GetFilePath((DATATYPE)_datatype, (LOCATIONS)_location, (MAPTYPE)_mapType);
            ZapiszMapeDoPliku(tempDict,path);
            Console.WriteLine("Aktualizacja z istniejącymi danymi");

           // int bazaWszystkichMapKeyToRefference = Constants.GetKeyFromMapLocationAndType(_location, _mapType);
            LoadMapDataFromFile ((LOCATIONS)_location, (MAPTYPE)_mapType, path );

            // inkrementacja numeru update'a
            // TODO: zaktualizować Versje dla konkretnej mapki w patchnotesie
            UpdateChecker.SERVER_UPDATE_VERSIONS._Data[_location][_mapType].UpdateVersionNumber();
            UpdateChecker.SaveUpdatesChangesToFile();
            
        }

        public static void ZapiszMapeDoPliku(Dictionary<Vector3, string> mapData, string path)
        {
            Console.WriteLine("Zapisywanie danych mapy do pliku");
            using (FileStream fs = new FileStream(path, FileMode.Truncate))
            {
                using (TextWriter tw = new StreamWriter(fs))
                    
                foreach (KeyValuePair<Vector3, string> kvp in mapData)
                {
                    tw.WriteLine(string.Format("{0} {1}", kvp.Key, kvp.Value));
                }
            }
        }   
        public static void LoadMapDataFromFile(LOCATIONS _location, MAPTYPE _mapType, string path)
        {
            Console.WriteLine($"Ladowanie danych mapy[{_mapType.ToString()}] z pliku do pamięci");
            var mapData = new Dictionary<Vector3,string>();
            if (!File.Exists(path)) 
            {
                Console.WriteLine(path);
                Console.WriteLine("Plik nie istnieje");
                return;
            }
            // ----------------------------------ZCZYTYWANIE Z PLIKU ----------------------------------
            string line;

            int modifiedCounter = 0, wrongDataRecords = 0, deletedCounter = 0, newAddedCounter = 0;

            StreamReader file = new StreamReader(path);  
            while((line = file.ReadLine()) != null)  
            {  
                string text = line.Replace("<","").Replace(">","");
                string[] data = text.Split(" ");

                try {
                    int x = Int32.Parse(data[0].Trim().Replace(",", ""));
                    int y = Int32.Parse(data[1].Trim().Replace(",", ""));
                    int z = Int32.Parse(data[2].Trim().Replace(",", ""));
                    string value = data[3];

                    mapData.Add(new Vector3(x, y, z), value);
                }
                catch(System.FormatException ex) {
                    Console.WriteLine("zły format, zle załądowana lokalizacja Vector3 => "+text+" Error: "+ex.Message );
                    wrongDataRecords++;
                }
                catch(Exception ex) {
                    Console.WriteLine("load map data"+ex.Message);
                }
            }  
            file.Close();

            int key = Constants.GetKeyFromMapLocationAndType(_location, _mapType);
            Dictionary<Vector3, string> REFERENCEMAP = Server.BazaWszystkichMDanychMap[key];
            if (REFERENCEMAP.Count == 0) 
            {
                REFERENCEMAP = mapData;
            }
            // ---------- MODYFIKACJA ISTNIEJĄCYCH DANYCH SERVERA
            if (REFERENCEMAP.Count > 0) 
            {
        if (mapData.Count == 0) Console.WriteLine("Plik jest pusty -> Brak zapisanych danych mapy");

                // porownanie i dodanie/zamiana danych z istniejącym zapisem w pamiec
                foreach (var kvp in mapData) {
                    if (REFERENCEMAP.ContainsKey(kvp.Key)) {
                        if (REFERENCEMAP[kvp.Key] != kvp.Value) {
                        REFERENCEMAP[kvp.Key] = kvp.Value;
                            modifiedCounter++;
                        }
                    }else {
                        REFERENCEMAP.Add(kvp.Key, kvp.Value);
                        newAddedCounter++;
                    }
                }

                // usuniecie nieaktualnych pól
                foreach (var pole in REFERENCEMAP.Where(pole => mapData.ContainsKey(pole.Key) == false).Select(pole => pole.Key).ToList()) 
                {
                    REFERENCEMAP.Remove(pole);
                    deletedCounter++;
                }
            }

           // ----------------------------------PODSUMOWANIE ----------------------------------
            Console.WriteLine(
                $"Odczytano: .................. {mapData.Count}\n"+
                $"Dodano: ..................... {newAddedCounter}\n" +
                $"Zmodyfikowano: .............. {modifiedCounter}\n" +
                $"Usunięto: ................... {deletedCounter}\n" +
                $"Uszkodzonych danych: ........ {wrongDataRecords}");

            Server.BazaWszystkichMDanychMap[key] = REFERENCEMAP;
        }

        public static void ChangePlayerLocalisation(int _fromClient, Packet _packet)
        {
            LOCATIONS _location = (LOCATIONS)_packet.ReadInt();
            Server.clients[_fromClient].player.CurrentLocation = _location;
            Console.WriteLine($"Gracz [{Server.clients[_fromClient].player.Username}] zmienił mapę na: [{_location.ToString()}]");

/*
TODO:
    - pobranie od admina nowej mapki i rozesłanie jej do innych klientow
    
    - lista graczy ma zawierac info o aktualnej swojej lokalizacji
    - zmodyfikowac weryfikacje poruszania sie uwzględniając dane odpowiednie dla odpowiadającej graczu lokalizacji
    - jezeli gracz ma nieaktualną lokalizacje wysłać mu aktualizacje
    - //TODO: CZY GRACZ NA INNEJ MAPIE POWIENIEN DOSTAWAĆ INFO O INNYCH GRACZACH ?
        -- w przypadku mapy z 'widokiem na inne piętro = tak"
        -- zamknięte pomieszczenie = nie, do czasu ew. włączenia punktu widokowego
        --  
    - ...




*/

        }

        public static void SendNumberOfLAtestMapUpdate(int _fromClient, Packet _packet)
        {
         //   Console.WriteLine("Wyslanie do gracza info zawierające aktualny numer update'a");
           //  int _id = _packet.ReadInt();
            ServerSend.SendCurrentUpdateVersionNumber(sendToID: _fromClient);
        }

       public static void SendUpdatedVersionMapDataToClient(int _FromClient, Packet _packet) {
            bool _isRequestSpecified = _packet.ReadBool();
            
            int _id = _packet.ReadInt();
            var LocationCount = Enum.GetNames(typeof(LOCATIONS)).Length;
            var mapTypesCount = Enum.GetNames(typeof(MAPTYPE)).Length;
            if (_isRequestSpecified == false) {
                // WYSYŁAMY WSZYSTKO CO MAMY
                Console.WriteLine("wysłanie do gracza " + _id + " mapy w ilości: " + (LocationCount * mapTypesCount));

                for (int location = 0; location < LocationCount; location++) {
                    for (int maptype = 0; maptype < mapTypesCount; maptype++) {
                        int key = Constants.GetKeyFromMapLocationAndType((LOCATIONS)location, (MAPTYPE)maptype);

                        ServerSend.SendMapDataToClient(_id, (LOCATIONS)location, (MAPTYPE)maptype, Server.BazaWszystkichMDanychMap[key]);
                    }
                }
            }
            else {
                // WYSYŁAMY KONKRETNY PAKIET MAPY
                LOCATIONS _location = (LOCATIONS)_packet.ReadInt();
                MAPTYPE _maptype = (MAPTYPE)_packet.ReadInt();

                int key = Constants.GetKeyFromMapLocationAndType(_location, _maptype);

                Console.WriteLine($"wysyłanie: LOCATION:{_location} / MAPTYPE:{_maptype}");
               // Console.WriteLine($"wysłanie do gracza {_id} konkretnej mapy [{_location.ToString()}][{_maptype.ToString()}]");
                //Console.WriteLine("serwer przechowuje w pamięci "+Server.BazaWszystkichMDanychMap.Count +" map.");
                ServerSend.SendMapDataToClient(_id, _location, _maptype, Server.BazaWszystkichMDanychMap[key]);
                }
            }
        }
    }

// TODO: wysyłanie przez uzytkownika tylko komendy na serwer prostym żądaniem pingCommand
