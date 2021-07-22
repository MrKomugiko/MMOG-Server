using System;
using System.Collections.Generic;
using System.Numerics;

namespace MMOG
{
    public class DungeonGate
    {
        public int GateID;
        public DUNGEONS Location;
        public List<Vector3> PositionsOnMap;
        // zakladając ze kazdego gate otwiera sie trigerkiem
        public Vector3 TriggerPosition;
        public bool GateStatus; // False = nieaktywna ( mozna przejsc ), True = aktywna (przejscie)
        public DungeonGate(int _gateID, DUNGEONS _location, List<Vector3> _positionsOnMap, bool _gateStatus, Vector3 _triggerPosition)
        {
            GateID = _gateID;
            Location = _location;
            PositionsOnMap = _positionsOnMap;
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
            foreach(var position in PositionsOnMap)
            {
                var oldTile = Server.BazaWszystkichMDanychMap[Constants.GetKeyFromMapLocationAndType(DungeonLocation,MAPTYPE.Ground_MAP)][position];
                currentGroundData[position] = oldTile;
            }
            
            // TODO: wysłanie do graczy z tego pokoju pingu o zmiane gate na puste pole lub odpalenie animacji otwierania sie 
        }
 
    }
}