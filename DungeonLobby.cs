using System;
using System.Collections.Generic;
using System.Numerics;

namespace MMOG
{
    public class DungeonLobby
    {
        public int LobbyID { get; private set; }
        public Player LobbyOwner { get; private set; }
        public LOCATIONS DungeonLocation { get; private set; }
        public List<Player> Players { get; set; } = new List<Player>();
        public int PlayersCount { get => Players.Count; }
        private Dictionary<Vector3, string> DungeonMAPDATA { get; set; } = new Dictionary<Vector3, string>();
        private Dictionary<Vector3, string> DungeonMAPDATA_Ground { get; set; }= new Dictionary<Vector3, string>();

        public DungeonLobby(int _lobbyID, Player _lobbyOwner, LOCATIONS _dungeonLocation)
        {
            Console.WriteLine("utworzono nowe lobby dungeonu");
            Console.WriteLine("Założycielem lobby jest : "+ _lobbyOwner.Username);
            LobbyID = _lobbyID;                 // powinno byc unikalne
            LobbyOwner = _lobbyOwner;           // unikalny
            DungeonLocation = _dungeonLocation; // jeden na lobby
            Players.Add(LobbyOwner);            // na starcie wliczajac zalozyciela do puli graczy
            
            Console.WriteLine("Ładowanie kopi danych mapy");
            DungeonMAPDATA = Server.BazaWszystkichMDanychMap[Constants.GetKeyFromMapLocationAndType(DungeonLocation,MAPTYPE.Obstacle_MAP)];
            DungeonMAPDATA_Ground = Server.BazaWszystkichMDanychMap[Constants.GetKeyFromMapLocationAndType(DungeonLocation,MAPTYPE.Ground_MAP)];
            Console.WriteLine($"załadowano:{DungeonMAPDATA.Count}[Obstacle] / {DungeonMAPDATA_Ground.Count}[Ground]");
        }

        public void Join(Player playerWhoWantJoin)
        {
            Players.Add(playerWhoWantJoin);

            Console.WriteLine(playerWhoWantJoin.Username +" dołącza do lobby");
        }

        public void Leave(Player playerWhoWantLeave)
        {
            if(Players.Contains(playerWhoWantLeave))
            {
                Players.Remove(playerWhoWantLeave);
                Console.WriteLine(playerWhoWantLeave.Username +" wychodzi z lobby");
            }
            else
            {
                Console.WriteLine("gracza nie ma na liscie graczy w lobby");
            }
            
        }
        public static void CreateNewLobby(int _fromClient, Packet _packet)
        {
            Player roomLeader = Server.clients[_fromClient].player;
            LOCATIONS dungeon = (LOCATIONS)_packet.ReadInt();
            Console.WriteLine($"tworzenie nowego pokoju - dungeon {dungeon.ToString()} przez gracza: {roomLeader.Username}");

            Server.dungeonLobbyRooms.Add(new DungeonLobby(1,roomLeader,dungeon));
        }
    }
}