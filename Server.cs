using System.ComponentModel;
using System.Reflection.Metadata;
using System.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Linq;

namespace MMOG
{
    class Server
    {

        public static List<SimplePlayerCreditionals> USERSDATABASE = new List<SimplePlayerCreditionals>{
            new SimplePlayerCreditionals(9000,"[GM]", "admin"),
        };
        public static List<Player> Players_DATABASE = new List<Player>(){
            new Player(0,"[GM]",9000)
        };
        public static int GetUserId(string _username) => Players_DATABASE.Where(user=>user.Username == _username).FirstOrDefault().UserID;
        public static Player GetPlayerByUserID(int _userId) =>  Players_DATABASE.Where(user=>user.UserID == _userId).FirstOrDefault();

        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers;
        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        public static Dictionary<int, string> listaObecnosci = new Dictionary<int, string>();
        // TODO: ważne index to [location*10+maptype+1]
        public static Dictionary<int,Dictionary<Vector3, string>> BazaWszystkichMDanychMap = new Dictionary<int, Dictionary<Vector3, string>>();

        public static Player GetPlayerData(int userID, int serverID)
        {
            // return current avaiable server ID;
            Player player= Players_DATABASE.Where(user => user.UserID == userID).First();
            player.Id = serverID;
            return player;
        }


        //public static Dictionary<Vector3, string> GLOBAL_MAPDATA = new Dictionary<Vector3, string>();
        //    public static Dictionary<Vector3, string> GROUND_MAPDATA = new Dictionary<Vector3, string>();
        [Obsolete] public static int UpdateVersion = 1001;
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

            //TODO: foreach na lokacjach
            //var dataTypesCount = Enum.GetNames(typeof(DATATYPE)).Length;
            var LocationCount = Enum.GetNames(typeof(LOCATIONS)).Length;
            var mapTypesCount = Enum.GetNames(typeof(MAPTYPE)).Length;

            DATATYPE datatype = DATATYPE.Locations;
            for (int location = 0 ; location < LocationCount ; location++)
            {
                for(int maptype = 0 ; maptype < mapTypesCount ; maptype++)
                {
                    int key = Constants.GetKeyFromMapLocationAndType((LOCATIONS)location,(MAPTYPE)maptype);
                    Dictionary<Vector3,string> dataRefference = new Dictionary<Vector3, string>();
                    if(BazaWszystkichMDanychMap.TryGetValue(key,out dataRefference) == false)
                    {
                        Console.WriteLine("Dodawanie wpisu do bazy danych map o kluczu: "+key);
                        BazaWszystkichMDanychMap.Add(key,new Dictionary<Vector3, string>());
                    }
                        
                    ServerHandle.LoadMapDataFromFile
                    (
                        (LOCATIONS)location,
                        (MAPTYPE)maptype,
                        Constants.GetFilePath(datatype, (LOCATIONS)location, (MAPTYPE)maptype)
                    );
                //   Console.WriteLine($"KEY:{((location*10)+maptype)} {((Locations)location).ToString()} / {((MAPTYPE)maptype).ToString()} / SIZE: {dataRefference.Count} ");
                }
            }
        }
        
        static public List<DungeonLobby> dungeonLobbyRooms = new List<DungeonLobby>();
        
        private static void TCPConnectCallback(IAsyncResult _result)
        {
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);

            for (int i = 1; i <= MaxPlayers; i++)
            {
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(_client);
                    //Console.WriteLine($"Incoming connection from {_client.Client.RemoteEndPoint}...");
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

        internal static void ShowDungeonLobbyRoomsInfo()
        {
            foreach(DungeonLobby room in dungeonLobbyRooms)
            {
                Console.WriteLine($"[{room.DungeonLocation.ToString()}]");
                foreach(Player player in room.Players)
                {
                    if(player == room.LobbyOwner) 
                        Console.WriteLine($"\t{player.Username} [Leader]");
                    else 
                        Console.WriteLine($"\t{player.Username}");
                }
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
                { (int)ClientPackets.LogMeIn, ServerHandle.LoginDataReceived },
                { (int)ClientPackets.playerMovement, ServerHandle.PlayerMovement },
                { (int)ClientPackets.SendChatMessage, ServerHandle.SendChatMessage },
                { (int)ClientPackets.PingReceived, ServerHandle.PingReceived },
                { (int)ClientPackets.SEND_MAPDATA_TO_SERVER, ServerHandle.MapDataReceived },
                { (int)ClientPackets.downloadLatestMapUpdate, ServerHandle.SendUpdatedVersionMapDataToClient },
                { (int)ClientPackets.download_recentMapVersion, ServerHandle.SendNumberOfLAtestMapUpdate },
                { (int)ClientPackets.clientChangeLocalisation, ServerHandle.ChangePlayerLocalisation },
                { (int)ClientPackets.TeleportMe, ServerHandle.TeleportPlayerToLocation },
                { (int)ClientPackets.PlayerMakeAction, ServerHandle.ExecutePlayerAction },

                { (int)ClientPackets.InitDataDungeonLobby, DungeonLobby.SendInitDataToClient },
                { (int)ClientPackets.CreateLobby, DungeonLobby.CreateNewLobby },
                { (int)ClientPackets.RemoveLobby, DungeonLobby.RemoveExistingLobby },
                { (int)ClientPackets.JoinLobby, DungeonLobby.JoinToRoom },
                { (int)ClientPackets.LeaveRoomBylayer, DungeonLobby.LeaveRoomByPlayer },
                { (int)ClientPackets.GroupTeleport, ServerHandle.GRoupRoomPlayersTeleport }

                

                

            };

            Console.WriteLine("Initialized packets.");
        }

        public static void ZaktualizujListeObecnosci(int afkId) {
            listaObecnosci.Remove(afkId);
        }
    }

    class SimplePlayerCreditionals
    {
        public static int RegisteredUSersCount;
        public SimplePlayerCreditionals(int userID, string username, string password)
        {
            UserID = userID;
            Username = username;
            Password = password;

            RegisteredUSersCount++;
        }

        public int UserID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
