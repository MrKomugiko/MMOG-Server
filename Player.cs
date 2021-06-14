﻿using System.Globalization;
using System.Net;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MMOG
{
    class Player
    {
        #region variables
        private bool _RegisteredUser = false;
        private int _id;
        private int _userID;
        private string _username;
        private string _password;
        private Vector3 _position;
        private Quaternion _rotation;
        private LOCATIONS _currentLocation;
        private int currentFloor;
        private bool[] _inputs; // wciśnięte klawisze przez gracza

        public DateTime _registrationDate;
        public DateTime LastLoginDate;

        public bool RegisteredUser { get => _RegisteredUser; set => _RegisteredUser = value; }
        public int Id { get => _id; set => _id = value; }
        public int UserID { get =>_userID; set => _userID = value; }
        public string Username { get => _username; set => _username = value; }
        public string Password { get => _password; set => _password = value; }
        public Vector3 Position { get => _position; set => _position = value; }
        public LOCATIONS CurrentLocation { get => _currentLocation; set => _currentLocation = value; }
        public bool[] Inputs { get => _inputs; set => _inputs = value; }
        public Quaternion Rotation { get => _rotation; set => _rotation = value; }
        public int CurrentFloor { get => currentFloor; set => currentFloor = value; }

        //  private float moveSpeed = 5f / Constants.TICKS_PER_SEC; // dlatego że serwer odbiera 30 wiadomości na sekunde
        // odpowiadałoby to speed / time.deltatime w unity

        #endregion
        
        public Player(int id, string username, int userID = 0000)
        {
            this.Id = id;
            this.UserID = Server.USERSDATABASE.Count()+1000;
            this.Username = username;
            this.Position = new Vector3(0,0,2);
            this.Rotation = Quaternion.Identity;
            this.LastLoginDate = DateTime.Now;
            this.CurrentLocation = LOCATIONS.Start_First_Floor;
            this.CurrentFloor = 2;
            this.Inputs = new bool[4];

            Console.WriteLine($"Dodano do bazy gracza [{this.UserID}][{this.Username}]");
        }
        public void Update()
        {
            Vector2 _inputDirection = Vector2.Zero;

            if (_inputs[0]) _inputDirection.Y += 1; // W
            if (_inputs[1]) _inputDirection.Y -= 1; // S
            if (_inputs[2]) _inputDirection.X -= 1; // A
            if (_inputs[3]) _inputDirection.X += 1; // D

            if (_inputDirection == Vector2.Zero) return;

            // rusz sie tylko przy zarejestrowanym ruchu roznim niz 0.0
            Move(_inputDirection);
        }
        private bool walkIntoStairs;
        private void Move(Vector2 _direction)
        {
            Inputs = new bool[4];

            Vector3 newPosition = Position + new Vector3(_direction.X, _direction.Y, 0);
          //  Console.WriteLine("player want move to: "+newPosition);
            if (CheckIfPlayerNewPositionIsPossible(newPosition))
            {
                Position += new Vector3(_direction.X, _direction.Y, 0);
                if (walkIntoStairs)
                {
                    Position += stairsDirection;
                    walkIntoStairs = false;
                    CurrentFloor += (int)stairsDirection.Z;
                }
                CheckForItems();
                
            }

            ServerSend.PlayerPosition(this);
        }
        private void CheckForItems()
        {
            // trzymac w pamieci liste samych itemkow i ich lokalizacjiss
            // tyczy sie to itemkow juz lezacych na ziemi - znajdziek 

            // drop bedzie sie pojawiac losowo z puli, ale to poznmiej
            int ObstacleMap_key = Constants.GetKeyFromMapLocationAndType(CurrentLocation, MAPTYPE.Obstacle_MAP);
            
            if(Server.BazaWszystkichMDanychMap[ObstacleMap_key].ContainsKey(Position))
            {
                if(Server.BazaWszystkichMDanychMap[ObstacleMap_key][Position].Contains("ITEM"))
                {
                    //Console.WriteLine($"|Gracz {username} znalazl {Server.GLOBAL_MAPDATA[position]}");

                    // TODO: ServerSend.CollectItem 
                        // usuniecie z bazy serwera oznaczenie jako nieaktywny?
                        //dodanie do gracza 
                }
            }

            return;
        }
        public void SetInput(bool[] _inputs, Quaternion _rotation)
        {
            
            Inputs = _inputs;
            this.Rotation = _rotation;
        }
        Vector3 stairsDirection;
        Vector3 Vector3FloorUP = new Vector3(0,0,2);
        Vector3 Vector3FloorDown = new Vector3(0,0,-2);
        
        private bool CheckIfPlayerNewPositionIsPossible(Vector3 _newPosition)
        {
            int GroundMap_key = Constants.GetKeyFromMapLocationAndType(CurrentLocation, MAPTYPE.Ground_MAP);
            int ObstacleMap_key = Constants.GetKeyFromMapLocationAndType(CurrentLocation, MAPTYPE.Obstacle_MAP);

            // proste sprawdzenie czy nastepna pozycją jest ściana lub czy istnieje
            Vector3 _groundPosition = _newPosition + Vector3FloorDown; // bo interesuje nas podłoga na ktorą gracz wchodzi
            Vector3 _downstairPosition = _newPosition + Vector3FloorDown + Vector3FloorDown; // bo interesuje nas podłoga na ktorą gracz wchodzi
            Vector3 currentPosition = Position + Vector3FloorDown;
            bool output = false;


            // jezeli chodzimy po ziemi jest git ;d
            if (Server.BazaWszystkichMDanychMap[GroundMap_key].ContainsKey(_groundPosition))
            {  
                if(Server.BazaWszystkichMDanychMap[GroundMap_key][_groundPosition].Contains("ground"))
                {
                    output= true;
                }
            }

                        // jezeli chodzimy po ziemi jest git ;d
            if (Server.BazaWszystkichMDanychMap[ObstacleMap_key].ContainsKey(_groundPosition))
            {  
                if(Server.BazaWszystkichMDanychMap[ObstacleMap_key][_groundPosition].Contains("schody"))
                {
                   // Console.WriteLine("Graczwchodzi na schodek -2L = poziom gracza");// => zwykłe przemeiszczenie sie w poziomo, w razie gdyby shcvody w pewnym momencie sie wydluzaly prosto ?
                    output = true;
                }
            }
            
            if (Server.BazaWszystkichMDanychMap[ObstacleMap_key].ContainsKey(_newPosition))
            {
                // jezeli przed nami jest klocek sciany => 
                 if (Server.BazaWszystkichMDanychMap[ObstacleMap_key][_newPosition].Contains("WALL")) {
                    output= false;
                 }

                // gracz ma przed sobą schodek i chce na neigo wejsc
                if(Server.BazaWszystkichMDanychMap[ObstacleMap_key][_newPosition].Contains("schody"))
                {
                    walkIntoStairs = true;
                    stairsDirection = Vector3FloorUP;
                    return output = true;
                }
            }
            
                //---------------------------------------------------
                // gracz schodzi na dol opuszczajac schody
            // jezeli w docelowym miejjscu za schodkiem jest ziemia
            if(Server.BazaWszystkichMDanychMap[GroundMap_key].ContainsKey(_downstairPosition))
            {
                // jezeli aktualnie stoimy na schodach
                if(Server.BazaWszystkichMDanychMap[ObstacleMap_key].ContainsKey(currentPosition))
                {
                    if(Server.BazaWszystkichMDanychMap[ObstacleMap_key][currentPosition].Contains("schody"))
                    {
//                        Console.WriteLine("gracz chce zejść ze schodow na ziemie");
                        walkIntoStairs = true;
                        stairsDirection = Vector3FloorDown;
                        return  output = true;
                    }
                }
            }

            if (Server.BazaWszystkichMDanychMap[ObstacleMap_key].ContainsKey(_downstairPosition))
            {
                // gracz ma na dole schodek i chce na neigo zejsc
                if(Server.BazaWszystkichMDanychMap[ObstacleMap_key][_downstairPosition].Contains("schody"))
                {
                    stairsDirection = Vector3FloorDown;
                    walkIntoStairs = true;
                  /////////  stairsDirection = Vector3FloorDown;
                    return output = true;
                }
            }
            return output;
        }
    }
}