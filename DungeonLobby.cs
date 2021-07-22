
using System.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace MMOG
{
    public partial class DungeonLobby
    {
        public int LobbyID { get; private set; }
        public Player LobbyOwner { get; private set; }
        public LOCATIONS DungeonLocation { get; private set; }
        public List<Player> Players { get; set; } = new List<Player>();
        public int PlayersCount { get => Players.Count; }
        public int MaxPlayersCapacity { get; private set; }
        public bool IsStarted { get; set; }

        public List<DungeonGate> listaGateow = new List<DungeonGate>()
        {
            new DungeonGate(1,DUNGEONS.DUNGEON_2,new List<Vector3>()
                {
                    new Vector3(-8,8,0),
                    new Vector3(-7,8,0),
                    new Vector3(-6,8,0)
                },
                true,
                new Vector3(-5,7,2)
            ),
            new DungeonGate(2,DUNGEONS.DUNGEON_2,new List<Vector3>()
                {
                    new Vector3(8,-2,0),
                    new Vector3(8,-3,0),
                    new Vector3(8,-4,0)
                },
                true,
                new Vector3(7,-1,2)
            ),
            new DungeonGate(3,DUNGEONS.DUNGEON_2,new List<Vector3>()
                {
                    new Vector3(-5,-12,0),
                    new Vector3(-6,-12,0),
                    new Vector3(-7,-12,0)
                },
                true,
                new Vector3(-3,-10,2)
            )
        // TODO: zautomatyzowac xd
        };
            public static Dictionary<Vector3,DUNGEONS> dungeonExitsDict = 
            new Dictionary<Vector3, DUNGEONS>(){
                {new Vector3(3,2,2), DUNGEONS.DUNGEON_1},
                {new Vector3(3,1,2), DUNGEONS.DUNGEON_1},

                {new Vector3(18,-2,10), DUNGEONS.DUNGEON_2},
                {new Vector3(18,-3,10), DUNGEONS.DUNGEON_2},
                {new Vector3(18,-4,10), DUNGEONS.DUNGEON_2}

                // TODO: zasysać z jsona na serwerze
            };

        public Dictionary<Vector3, string> DungeonMAPDATA { get; set; } = new Dictionary<Vector3, string>();
        public Dictionary<Vector3, string> DungeonMAPDATA_Ground;// { get; set; }= new Dictionary<Vector3, string>();
        public DUNGEONS Get_DUNGEONS()
        {
            DUNGEONS dungeon;
               Enum.TryParse<DUNGEONS>(DungeonLocation.ToString(),out dungeon);
             //  Console.WriteLine(DungeonLocation.ToString());
               return dungeon;
        }
        public DungeonLobby(int _lobbyID, Player _lobbyOwner, LOCATIONS _dungeonLocation,int _maxPlayersCapacity = 2)
        {
          //  Console.WriteLine("utworzono nowe lobby dungeonu");
          //  Console.WriteLine("Założycielem lobby jest : "+ _lobbyOwner.Username);
            LobbyID = _lobbyID;                 // powinno byc unikalne
            LobbyOwner = _lobbyOwner;           // unikalny
            DungeonLocation = _dungeonLocation; // jeden na lobby
            MaxPlayersCapacity = _maxPlayersCapacity;
            Players.Add(LobbyOwner);   
                   // na starcie wliczajac zalozyciela do puli graczy
            
            Console.WriteLine("Ładowanie kopi danych mapy do obiektu lobby");
            DungeonMAPDATA = new Dictionary<Vector3,string>(Server.BazaWszystkichMDanychMap[Constants.GetKeyFromMapLocationAndType(DungeonLocation,MAPTYPE.Obstacle_MAP)]);
            DungeonMAPDATA_Ground = new Dictionary<Vector3,string>(Server.BazaWszystkichMDanychMap[Constants.GetKeyFromMapLocationAndType(DungeonLocation,MAPTYPE.Ground_MAP)]);

            // blokada przejscia spowodowana zamknietymi gateami \o.o/
            EnableClodesGates(Get_DUNGEONS());
          //  Console.WriteLine($"załadowano:{DungeonMAPDATA.Count}[Obstacle] / {DungeonMAPDATA_Ground.Count}[Ground]");
        }

        private void EnableClodesGates(DUNGEONS dungeonLoc)
        {

            foreach(DungeonGate gate in listaGateow.Where(g=>g.Location == dungeonLoc))
            {
                // usuwanie 'podlogi z pamieci w przypadku gdy gate jest aktywny, uniemolizi przez niego przejscie
                gate.PositionsOnMap.ForEach(p=> DungeonMAPDATA_Ground.Remove(p));
            }
        }

        // public void Join(Player playerWhoWantJoin)
        // {
        //     Players.Add(playerWhoWantJoin);

        //     Console.WriteLine(playerWhoWantJoin.Username +" dołącza do lobby");
        // }

        // public void Leave(Player playerWhoWantLeave)
        // {
        //     if(Players.Contains(playerWhoWantLeave))
        //     {
        //         Players.Remove(playerWhoWantLeave);
        //         Console.WriteLine(playerWhoWantLeave.Username +" wychodzi z lobby");
        //     }
        //     else
        //     {
        //         Console.WriteLine("gracza nie ma na liscie graczy w lobby");
        //     }

        // }
        public static void CreateNewLobby(int _fromClient, Packet _packet)
        {
            //Console.WriteLine("tworzenie nowego lobby-servera");
            Player roomLeader = Server.clients[_fromClient].player;
            int roomId = _packet.ReadInt();                             // losowa wartosc miedzy <0; 1`000`000)
            LOCATIONS dungeon = (LOCATIONS)_packet.ReadInt();
            Console.WriteLine($"tworzenie nowego pokoju - dungeon {dungeon.ToString()} przez gracza: {roomLeader.Username}");

            Server.dungeonLobbyRooms.Add(new DungeonLobby(roomId,roomLeader,dungeon));
            
            ServerSend.SendCurrentUpdatedDungeonLobbyData();
        }

        public static void RemoveExistingLobby(int _fromClient, Packet _packet)
        {
            Console.WriteLine("zadanie rozeslania info o usunieciu pokoju");
            var roomLeader = Server.clients[_fromClient].player;

            var dungeon = (DUNGEONS)_packet.ReadInt();
            var roomID = _packet.ReadInt();
            RemoveDungeonLobby(roomLeader, dungeon, roomID);
        }

        public static void RemoveDungeonLobby(Player roomLeader, DUNGEONS dungeon, int roomID)
        {
            Console.WriteLine("removing lobby room");
            var _dungeonLobby = Server.dungeonLobbyRooms.Where(room => room.LobbyID == roomID).FirstOrDefault();

            if (_dungeonLobby != null)
            {
                if (_dungeonLobby.LobbyOwner == roomLeader)
                {
                    // wyciągniecie listy graczy i rozesłanie do nich info ze nie naleza juz do zadnego pokoju 
                    foreach (var player in _dungeonLobby.Players)
                    {
                        //pomin lidera 
                        if (player == roomLeader) continue;
                        ServerSend.KickedFromDungeonRoom(player.Id, _dungeonLobby);

                    }
                    Server.dungeonLobbyRooms.Remove(_dungeonLobby);
                    Console.WriteLine("usuniecie isniejacego lobby-servera");
                }
                else
                    Console.WriteLine("Dziwne, gracz, nie będący liderem pokoju, chce go usunac? [ nie powinno sie zdazyc ]");

                ServerSend.RemoveDeletedRoom(dungeon: dungeon, roomId: roomID, _dungeonLobby);
            }

            ServerSend.SendCurrentUpdatedDungeonLobbyData(dungeon: dungeon);
        }

        public static void RemoveExistingLobby(DungeonLobby _room)
        {
            Player roomLeader = _room.LobbyOwner; 
            DUNGEONS dungeon;
            Enum.TryParse<DUNGEONS>(_room.DungeonLocation.ToString(),out dungeon);
          
            int roomID = _room.LobbyID;

            RemoveDungeonLobby(roomLeader, dungeon, roomID);
        }

        public static void JoinToRoom(int _fromClient, Packet _packet)
        {
            Player playerWhoJoin = Server.clients[_fromClient].player;

            int roomID = _packet.ReadInt();

            Console.WriteLine($"gracz {playerWhoJoin.Username} dołączył do pokoju o id:{roomID}");
            Server.dungeonLobbyRooms.Where(room=>room.LobbyID == roomID).First().Players.Add(playerWhoJoin);

            DungeonLobby room = Server.dungeonLobbyRooms.Where(room=>room.LobbyID == roomID).First();
            DUNGEONS dungeon;
            Enum.TryParse<DUNGEONS>(room.DungeonLocation.ToString(),out dungeon);
           
            foreach(var player in room.Players)
            {
              //  Console.WriteLine($"powiadomienie gracza: {player.Username}, ze dołączył nowy gracz trzeba zaktualizowac liste");

                ServerSend.SendCurrentUpdatedDungeonLobbyData(_toClient:player.Id, dungeon:dungeon, _action:"PlayerJoinToRoom");
            }
        }
        public static void LeaveRoomByPlayer(int _fromClient, Packet _packet)
        {           
                Player playerWhoLeaved = Server.clients[_fromClient].player;
                int roomID = _packet.ReadInt();

                var room = Server.dungeonLobbyRooms.Where(room=>room.LobbyID == roomID).FirstOrDefault();
                if(room == null){ 
                    Console.WriteLine("nie mozna opuscic pokoju, gdy ten nie isnieje");
                    return;
                }
                Console.WriteLine($"gracz {playerWhoLeaved.Username} opuscil pokoj z id:{roomID}");
               
                room.Players.Remove(playerWhoLeaved);
           //     Console.WriteLine("usuniecie gracza z listy uczestnikow pokoju");
                DUNGEONS dungeon;
                Enum.TryParse<DUNGEONS>(Server.dungeonLobbyRooms.Where(room=>room.LobbyID == roomID).FirstOrDefault().DungeonLocation.ToString(),out dungeon);
                // wyslanie tego tylko do graczy w pokoju zeby przeładowali liste i usuneli stary obiekt
                foreach(var player in room.Players)
                {
                   // Console.WriteLine($"powiadomienie gracza: {player.Username}, ze gracz wyszeedł i trzeba zaktualizowac liste");

                    ServerSend.SendCurrentUpdatedDungeonLobbyData(_toClient:player.Id, dungeon:dungeon, _action:"PlayerLeftRooom");
                }

        }
        internal static void LockRoomBecauseAlreadyStartes(int _fromClient, Packet _packet)
        {
            Console.WriteLine("wyslanie pingu o zablokowanie dostepu do pokoju bo jest w trakcie gry");
            int LobbyID = _packet.ReadInt();

            DungeonLobby lobby = Server.dungeonLobbyRooms.Where(room=>room.LobbyID == LobbyID).FirstOrDefault();   
            DUNGEONS dungeon;
            Enum.TryParse<DUNGEONS>(lobby.DungeonLocation.ToString(),out dungeon);
            lobby.IsStarted = true;
            ServerSend.SendCurrentUpdatedDungeonLobbyData(dungeon:dungeon, _action:"GameStarted");
         
        }
        public static void SendInitDataToClient(int _fromClient, Packet _packet)
        {
          //  Console.WriteLine("wyslano dane inicializujace liste dungeonow-lobby do nowego gracza");
            DUNGEONS dungeon = (DUNGEONS)_packet.ReadInt();
            ServerSend.SendCurrentUpdatedDungeonLobbyData(_fromClient, dungeon);
        }
    }
}