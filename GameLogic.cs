﻿using System;
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
            ThreadManager.UpdateMain();
        }
    }
}