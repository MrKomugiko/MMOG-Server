using System;
using System.Linq;
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
                if (consoleCommand == "cmd_help") ShowConsoleCommands();
                if (consoleCommand == "cmd_chat") GMMessagesToClients();
                if (consoleCommand == "cmd_users") ShowCurrentlyLoggedInUsers();

            }

        }

        private static void ShowCurrentlyLoggedInUsers() {
            string listaGraczy = "";
            foreach(Client client in Server.clients.Values)
            {
                if (client.player == null) continue;

                Player player = client.player;
                string timePlayerIsOnline = (DateTime.Now - player.loginDate).ToString(@"hh\:mm\:ss");
                listaGraczy += $"[#{player.id}]:[{player.username}]:[{timePlayerIsOnline}]\n";
            }

            Console.WriteLine(listaGraczy);
        }

        private static void ShowConsoleCommands()
        {
            Console.WriteLine(
                  "Dostepne komendy:\n"
                + "[cmd_chat]  -> wiadomosci GM na czacie globalnym.\n"
                + "[cmd_help]  -> wyswietlenie wszystkich komend\n"
                + "[cmd_users] -> lista aktualnie zalogowanych graczy\n");
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
                        try { Thread.Sleep(_nextLoop - DateTime.Now); } 
                        catch(Exception ex) { Console.WriteLine(ex.Message); };
                    }
                }
            }
        }
    }
}
