using System;
using System.Threading;

namespace MMOG
{
    class Program
    {
        private static bool isRunning = false;

        static void Main(string[] args)
        {
            Console.Title = "Game Server";
            isRunning = true;

            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();

            Server.Start(50, 5555);

            while(true)
            {
                string consoleCommand = Console.ReadLine();
                if(consoleCommand == "cmd_help") ShowConsoleCommands();
                if(consoleCommand == "cmd_chat") GMMessagesToClients();
            }

        }

        private static void ShowConsoleCommands()
        {
            Console.WriteLine(
                $"Dostepne komendy:\n"
                +"[cmd_chat] -> wiadomosci GM na czacie globalnym.\n"
                +"[cmd_help] -> wyswietlenie wszystkich komend\n");
        }
        private static void GMMessagesToClients()
        {
            string message = "";
            Console.WriteLine("wprowadz wiadomosc do ludu : \n[cmd_exit] -> by zakońćzyć nadawanie obwieszczeń");
            while(true) {
                message = Console.ReadLine();
                
                if(message == "cmd_exit") {
                    Console.Clear();
                    return;
                }
                ServerSend.UpdateChat(message);
            }
        }

        private static void MainThread()
        {
            Console.WriteLine($"Main thread started. Running at {Constants.TICKS_PER_SEC} ticks per second.");
            DateTime _nextLoop = DateTime.Now;

            while (isRunning)
            {
                while (_nextLoop < DateTime.Now)
                {
                    GameLogic.Update();

                    _nextLoop = _nextLoop.AddMilliseconds(Constants.MS_PER_TICK);

                    if (_nextLoop > DateTime.Now)
                    {
                        Thread.Sleep(_nextLoop - DateTime.Now);
                    }
                }
            }
        }
    }
}
