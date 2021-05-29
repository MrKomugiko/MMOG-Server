using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MMOG
{
    class Player
    {
        public int id;
        public string username;
        public DateTime loginDate;

        public Vector3 position;
        public Quaternion rotation;

        private float moveSpeed = 5f / Constants.TICKS_PER_SEC; // dlatego że serwer odbiera 30 wiadomości na sekunde
                                                                // odpowiadałoby to speed / time.deltatime w unity
        private bool[] inputs; // wciśnięte klawisze przez gracza
        public Player(int _id, string _username, Vector3 _spawnPosition) {
            id = _id;
            username = _username;
            position = _spawnPosition;
            rotation = Quaternion.Identity;
            loginDate = DateTime.Now;
            inputs = new bool[4];
        }

        public void Update() {
            Vector2 _inputDirection = Vector2.Zero;
     
            if (inputs[0]) _inputDirection.Y += 1; // W
            if (inputs[1]) _inputDirection.Y -= 1; // S
            if (inputs[2]) _inputDirection.X -= 1; // A
            if (inputs[3]) _inputDirection.X += 1; // D
            
            Move(_inputDirection);
        }

        private void Move(Vector2 _direction) {
            inputs = new bool[4];
            position += new Vector3(_direction.X, _direction.Y,0);

            // sprawdzenei czy nowa pozycja jest prawidłowa
            position = CheckIfPlayerNewPositionIsPossible(position) ? position : (position -= new Vector3(_direction.X, _direction.Y, 0));

            if(CheckIfPlayerNewPositionIsPossible(position)) ServerSend.PlayerPosition(this);
           //if (_direction != Vector2.Zero) ServerSend.PlayerPosition(this);
        }

        public void SetInput(bool[] _inputs, Quaternion _rotation) {
            inputs = _inputs;
            rotation = _rotation;
        }

        private bool CheckIfPlayerNewPositionIsPossible(Vector3 _newPosition) {
            // proste sprawdzenie czy nastepna pozycją jest ściana lub czy istnieje
            Vector3 _groundPosition = new Vector3(_newPosition.X, _newPosition.Y, _newPosition.Z - 2); // bo interesuje nas podłoga na ktorą gracz wchodzi

            if (Server.MAPDATA.ContainsKey(_newPosition) == true) {// jeżeli cokolwiek jest na posiomie gracza
                if (Server.MAPDATA[_newPosition] == "WALL") {// jezeli na poziomie gracza stoi sciana
                    //Console.WriteLine($"{Server.MAPDATA[_newPosition]} na pozycji {_newPosition} uniemozliwia przejscie. ");
                    return false; 
                }
            }

            if (Server.MAPDATA.ContainsKey(_groundPosition) == false) {// brak podlogi gracz nie wejdzie
                //Console.WriteLine($"Brak pola na pozycji {_newPosition} przejscie niemozliwe. ");
                return false; 
               }

            // wszystko w porządku ;d można iść
            return true;
        }
    }
}
