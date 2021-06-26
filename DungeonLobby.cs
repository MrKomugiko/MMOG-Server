
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace MMOG
{
    public class DungeonLobby
    {
        public int LobbyID { get; private set; }
        public Player LobbyOwner { get; private set; }
        public LOCATIONS DungeonLocation { get; private set; }
        public List<Player> Players { get; set; } = new List<Player>();
        public int PlayersCount { get => Players.Count; }
        public int MaxPlayersCapacity { get; private set; }

        private Dictionary<Vector3, string> DungeonMAPDATA { get; set; } = new Dictionary<Vector3, string>();
        private Dictionary<Vector3, string> DungeonMAPDATA_Ground { get; set; }= new Dictionary<Vector3, string>();

        public enum DUNGEONS // nazwa jednakowa z tą w locations !
        {
            DUNGEON_1
        }
        public DungeonLobby(int _lobbyID, Player _lobbyOwner, LOCATIONS _dungeonLocation,int _maxPlayersCapacity = 2)
        {
            Console.WriteLine("utworzono nowe lobby dungeonu");
            Console.WriteLine("Założycielem lobby jest : "+ _lobbyOwner.Username);
            LobbyID = _lobbyID;                 // powinno byc unikalne
            LobbyOwner = _lobbyOwner;           // unikalny
            DungeonLocation = _dungeonLocation; // jeden na lobby
            MaxPlayersCapacity = _maxPlayersCapacity;
            Players.Add(LobbyOwner);            // na starcie wliczajac zalozyciela do puli graczy
            
            Console.WriteLine("Ładowanie kopi danych mapy");
            DungeonMAPDATA = Server.BazaWszystkichMDanychMap[Constants.GetKeyFromMapLocationAndType(DungeonLocation,MAPTYPE.Obstacle_MAP)];
            DungeonMAPDATA_Ground = Server.BazaWszystkichMDanychMap[Constants.GetKeyFromMapLocationAndType(DungeonLocation,MAPTYPE.Ground_MAP)];
            Console.WriteLine($"załadowano:{DungeonMAPDATA.Count}[Obstacle] / {DungeonMAPDATA_Ground.Count}[Ground]");
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
            Console.WriteLine("tworzenie nowego lobby-servera");
            Player roomLeader = Server.clients[_fromClient].player;
            int roomId = _packet.ReadInt();                             // losowa wartosc miedzy <0; 1`000`000)
            LOCATIONS dungeon = (LOCATIONS)_packet.ReadInt();
            Console.WriteLine($"tworzenie nowego pokoju - dungeon {dungeon.ToString()} przez gracza: {roomLeader.Username}");

            Server.dungeonLobbyRooms.Add(new DungeonLobby(roomId,roomLeader,dungeon));
            
            ServerSend.SendCurrentUpdatedDungeonLobbyData();
        }

        public static void RemoveExistingLobby(int _fromClient, Packet _packet)
        {
            var roomLeader = Server.clients[_fromClient].player;

            var dungeon = (LOCATIONS)_packet.ReadInt();
            var roomID = _packet.ReadInt();
            var _dungeonLobby = Server.dungeonLobbyRooms.Where(room=>room.LobbyID == roomID).FirstOrDefault();

            if(_dungeonLobby != null)
            {
                if(_dungeonLobby.LobbyOwner == roomLeader)
                {
                    // wyciągniecie listy graczy i rozesłanie do nich info ze nie naleza juz do zadnego pokoju 
                    foreach(var player in _dungeonLobby.Players)
                    {
                        //pomin lidera 
                        if(player == roomLeader) continue;
                        ServerSend.KickedFromDungeonRoom(player.Id, _dungeonLobby);
                  
                    }
                    Server.dungeonLobbyRooms.Remove(_dungeonLobby);
                    Console.WriteLine("usuniecie isniejacego lobby-servera");
                }
                else
                    Console.WriteLine("Dziwne, gracz, nie będący liderem pokoju, chce go usunac? [ nie powinno sie zdazyc ]");
            }

             ServerSend.SendCurrentUpdatedDungeonLobbyData();
        }

        public static void JoinToRoom(int _fromClient, Packet _packet)
        {
            Player player = Server.clients[_fromClient].player;

            Server.dungeonLobbyRooms.Where(room=>room.LobbyID == _packet.ReadInt()).First().Players.Add(player);

            ServerSend.SendCurrentUpdatedDungeonLobbyData();
        }
    }
}