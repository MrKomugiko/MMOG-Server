using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MMOG
{
    class ServerHandle
    {
        public static void WelcomeReceived(int _fromClient, Packet _packet)
        {
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();

            Console.WriteLine($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
            if (_fromClient != _clientIdCheck)
            {
                Console.WriteLine($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
            }
            Server.clients[_fromClient].SendIntoGame(_username);
        }

        public static void UDPTestReceived(int _fromClient, Packet _packet)
        {
            string _msg = _packet.ReadString();

            Console.WriteLine($"Received packet via UDP. Contains message: {_msg}");
        }

        public static void PlayerMovement(int _fromClient, Packet _packet) {
            bool[] _inputs = new bool[_packet.ReadInt()]; // pobieranie wielości tablicy
            for(int i=0;i<_inputs.Length;i++) {
                _inputs[i] = _packet.ReadBool(); // pobieranie kolejnych wartości bool
            }
            Quaternion _rotation = _packet.ReadQuaternion(); // pobieranie rotacji

            Server.clients[_fromClient].player.SetInput(_inputs, _rotation); // przesłanie informacji dot. wcisnietych input klawiszy danego clienta
                                                                             
        }
    }
}
