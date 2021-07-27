using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MMOG
{
    public class DungeonGate
    {
        public int LobbyID;
        public int GateID;
        public DUNGEONS Location;
        public List<Vector3> GroundPositionsOnMap;
        public Vector3 MainGateTilePositionsOnMap;
        public Vector3 TriggerPosition;
        public bool GateStatus; // False = nieaktywna ( mozna przejsc ), True = aktywna (przejscie)
        public DungeonGate(int _lobbyID, int _gateID, DUNGEONS _location, Vector3 _mainGateTilePositionsOnMap, List<Vector3> _positionsOnMap,  bool _gateStatus, Vector3 _triggerPosition)
        {
            LobbyID = _lobbyID;
            GateID = _gateID;
            Location = _location;
            MainGateTilePositionsOnMap = _mainGateTilePositionsOnMap;
            GroundPositionsOnMap = _positionsOnMap;
            GateStatus = _gateStatus;
            TriggerPosition = _triggerPosition;
        }

        public void OPENGATE(ref Dictionary<Vector3, string> currentGroundData)
        {
            GateStatus = false;

            LOCATIONS DungeonLocation;
            Enum.TryParse<LOCATIONS>(this.Location.ToString(),out DungeonLocation);

            Console.WriteLine("Otwarcie bramy nr"+GateID);

            // wykonanie akcji umozliwiajace przejsccie przez brane
            // podstawienie starych danych pod miejsce gatea ( na nowo podlozenie drogi xd)
            foreach(var position in GroundPositionsOnMap)
            {
                var oldTile = Server.BazaWszystkichMDanychMap[Constants.GetKeyFromMapLocationAndType(DungeonLocation,MAPTYPE.Ground_MAP)][position];
                currentGroundData[position] = oldTile;
                Console.WriteLine("można juz przejść przez bramę nr"+GateID);
            }  

            TriggerAnimateOpening();
        }

        private void TriggerAnimateOpening()
        {
            // TODO: wysłanie do graczy z tego pokoju pingu o zmiane gate na puste pole lub odpalenie animacji otwierania sie 
            // pobieranie graczy w pokoju do ktorego nalezy ten gate
            var listaGraczyDoPoinformowania = Server.dungeonLobbyRooms.Where(r=>r.LobbyID == LobbyID).First().Players.Select(p=>p.Id).ToList();
            ServerSend.TrigerAnimationAndTileSwapProcedure(_playerIDList:listaGraczyDoPoinformowania,_tilePositionGrid:this.MainGateTilePositionsOnMap);
        }
    }
}