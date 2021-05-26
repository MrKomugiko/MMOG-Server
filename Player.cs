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
            position += new Vector3(_direction.X * moveSpeed, _direction.Y * moveSpeed,0);

            if(_direction != Vector2.Zero) Console.WriteLine($"[{username}] moved.");
            ServerSend.PlayerPosition(this);
        }

        public void SetInput(bool[] _inputs, Quaternion _rotation) {
            inputs = _inputs;
            rotation = _rotation;
        }
    }
}
