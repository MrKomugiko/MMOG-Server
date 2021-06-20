using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MMOG
{

    class GameLogic
    {
        public static void Update() {
            // Wszyscy istniejący clienci na serwerze
            // 
            foreach (Client _client in Server.clients.Values.Where(c => c.player != null)) 
            {
                try 
                {
                    _client.player.Update();
                } 
                catch (Exception ex) 
                {
                    Console.WriteLine(ex);
                }
            }

            RandomWalkServerPlayer();
            ThreadManager.UpdateMain();
        }
        static int delay = 15;
        private static void RandomWalkServerPlayer()
        {
            Random rnd = new Random();
            delay--;
            if(delay < 0){
                
                //tylko boty
                foreach(Client _client in Server.clients.Values.Where(c =>c.player != null && c.id > 40))
               {
                    int random = rnd.Next(0,5);
                    _client.player.Move(getDirection(random));
                }   

                delay = 15;
            }
        }
        static Vector2 getDirection(int directionNumbe)
        {
            switch(directionNumbe)
            {
                case 1:  return Vector2.UnitX;        
                case 2: return Vector2.UnitX;
                case 3: return -Vector2.UnitX;                
                case 4: return -Vector2.UnitY;
                default: return Vector2.Zero;
            }
        }
        public static void TeleportPlayer(int playerId, Vector3 location)
        {
            Server.clients[playerId].player.Teleport(location);
        }
    }
}