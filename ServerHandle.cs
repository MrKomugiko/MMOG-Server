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

        public static void UDPTestReceived(int _fromClient, Packet _packet) {
            string _msg = _packet.ReadString();

            Console.WriteLine($"Received packet via UDP. Contains message: {_msg}");
        }

        public static void PlayerMovement(int _fromClient, Packet _packet) {
            bool[] _inputs = new bool[_packet.ReadInt()]; // pobieranie wielości tablicy
            for (int i = 0; i < _inputs.Length; i++) {
                _inputs[i] = _packet.ReadBool(); // pobieranie kolejnych wartości bool
            }
            Quaternion _rotation = _packet.ReadQuaternion(); // pobieranie rotacji

           try
           {
            Server.clients[_fromClient].player.SetInput(_inputs, _rotation); // przesłanie informacji dot. wcisnietych input klawiszy danego clienta                                                                    
           }
           catch (System.Exception ex)
           {
                Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] Error: {ex.Message} ");               
           } 
        }

        public static void SendChatMessage(int _fromClient, Packet _packet) {
            //tutaj obieram wiadomość od klienta i rozsyłam ją do wszystkich aby ją zaktualizowali na czacie
            int _id = _packet.ReadInt();
            string _msg = _packet.ReadString();
            Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}]:[{Server.clients[_id].player.username}]:{_msg}");

            ServerSend.UpdateChat_NewUserPost(_id, _msg);
        }


        public static void PingReceived(int _fromClient, Packet _packet) {
          try {
                Server.listaObecnosci[_fromClient] = $"[.OK.] \t[#{_fromClient} {Server.clients[_fromClient].player.username}]";
            } 
            catch(Exception ex) {
                Console.WriteLine("Cos poszło nie tak z aktualizacją statusu obecności gracza" + ex.Message);
           }
        }

        public static void MapDataReceived(int _fromClient, Packet _packet) {
            Dictionary<Vector3, string> tempDict = new Dictionary<Vector3, string>();
            int _dataSize = _packet.ReadInt();
            MAPTYPE _mapTypeName = (MAPTYPE)_packet.ReadInt();
            string path = "";
            switch (_mapTypeName)
            {
                case MAPTYPE.GROUND_MAP:
                    Console.WriteLine("Odebrano dane mapy typu GROUND.");
                    path = Constants.GROUND_MAP_DATA_FILE_PATH;
                break;

                case MAPTYPE.OBSTACLEMAP:
                    Console.WriteLine("Odebrano dane mapy typu GLOBAL/OBSTACLE.");
                    path = Constants.MAP_DATA_FILE_PATH;
                break;
            }
            for (int i = 0; i < _dataSize; i++) 
            {
                var key = _packet.ReadVector3();
                string value = _packet.ReadString();
                tempDict.Add(key, value);
            }

            ZapiszMapeDoPliku(tempDict,path);
            Console.WriteLine("Aktualizacja z istniejącymi danymi");
            LoadMapDataFromFile(_mapTypeName, path);

            // inkrementacja numeru update'a
            Server.UpdateVersion++;
        }

        private static void ZapiszMapeDoPliku(Dictionary<Vector3, string> mapData, string path)
        {
            Console.WriteLine("Zapisywanie danych mapy do pliku");
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                using (TextWriter tw = new StreamWriter(fs))
                    
                foreach (KeyValuePair<Vector3, string> kvp in mapData)
                {
                    tw.WriteLine(string.Format("{0} {1}", kvp.Key, kvp.Value));
                }
            }
        }   
        public static void LoadMapDataFromFile(MAPTYPE _mapType, string path)
        {

            Console.WriteLine($"Ladowanie danych mapy[{_mapType.ToString()}] z pliku do pamięci");
            var mapData = new Dictionary<Vector3,string>();
            if (!File.Exists(path)) 
            {
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
                    Console.WriteLine(ex.Message);
                }
            }  
            file.Close();
            
            Dictionary<Vector3,string> REFERENCEMAP = new Dictionary<Vector3,string>();
            switch (_mapType)
            {
                case MAPTYPE.GROUND_MAP:
                    REFERENCEMAP = Server.GROUND_MAPDATA;
                break;
                case MAPTYPE.OBSTACLEMAP:
                    REFERENCEMAP = Server.GLOBAL_MAPDATA;      
                break;   
            }
            // ----------------------------------ZAPISYWANIE W PAMIECI SERVERA ----------------------------------
            // --------- JEZELI NIE MA ZAPISANYCH DANYCH NA SERWERZE
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
            //---------- przypisanie danych 
            switch (_mapType)
            {
                case MAPTYPE.GROUND_MAP:
                    Server.GROUND_MAPDATA = REFERENCEMAP;
                break;
                case MAPTYPE.OBSTACLEMAP:
                    Server.GLOBAL_MAPDATA = REFERENCEMAP;    
                break;   
            }

           // ----------------------------------PODSUMOWANIE ----------------------------------
            Console.WriteLine(
                $"Odczytano: .................. {mapData.Count}\n"+
                $"Dodano: ..................... {newAddedCounter}\n" +
                $"Zmodyfikowano: .............. {modifiedCounter}\n" +
                $"Usunięto: ................... {deletedCounter}\n" +
                $"Uszkodzonych danych: ........ {wrongDataRecords}");
        }   
    
        public static void SendNumberOfLAtestMapUpdate(int _fromClient, Packet _packet)
        {
            Console.WriteLine("Wyslanie do gracza info zawierające aktualny numer update'a");
           //  int _id = _packet.ReadInt();
            ServerSend.SendCurrentUpdateVersionNumber(sendToID: _fromClient);
        }

       public static void SendLatestUpdateMapDataToClient(int _FromClient, Packet _packet) {
            int _id = _packet.ReadInt();
            Console.WriteLine("Otrzymanie żądania o wysłanie nowej mapy przez klienta #"+_id);

            ServerSend.SendMapDataToClient(_id);
        }
    }
}

// TODO: wysyłanie przez uzytkownika tylko komendy na serwer prostym żądaniem pingCommand
