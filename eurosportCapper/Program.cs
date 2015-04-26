using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace eurosportCapper
{
    static class Program
    {
        private static VLCRelay _relay;
        private static int _port;

        static void PrintHelp()
        {
            Console.WriteLine("Usage: eurosportCapper <username> <password> [channel] [language] [VLC]");
            Console.WriteLine("Username and password are the Eurosport Player login");
            Console.WriteLine("channel must be 0 for Eurosport and 1 for Eurosport2");
            Console.WriteLine("language must be GB, DE or FR");
            Console.WriteLine("VLC must be true or false; true starts VLC on the stream");
        }

        static void Main(string[] args)
        {
            bool startVLC = false;
            if (args.Length > 4)
                startVLC = bool.Parse(args[4]);

            bool isMono = Type.GetType("Mono.Runtime") != null;
            if (!isMono && startVLC)
            {
                var rand = new Random();
                _port = rand.Next(4200, 52000);
                _relay = new VLCRelay(_port);
            }

            if (args.Length < 2)
            {
                PrintHelp();
                return;
            }

            var username = args[0];
            var password = args[1];

            int channel = 0;
            if (args.Length > 2)
                channel = int.Parse(args[2]);
            var broadcast = Broadcast.British;
            if (args.Length > 3)
            {
                switch (args[3])
                {
                    case "GB":
                        broadcast = Broadcast.British;
                        break;

                    case "DE":
                        broadcast = Broadcast.German;
                        break;

                    case "FR":
                        broadcast = Broadcast.French;
                        break;
                }
            }

            bool first = true;
            while (true)
            {
                try
                {
                    var eurosport = new Eurosport(username, password, broadcast);
                    eurosport.Login();
                    var channels = eurosport.GetAllProducts();

                    var eurosport2 = channels[channel];
                    Console.WriteLine("Currently: " + eurosport2.LiveLabel + " - " + eurosport2.Label);
                    var item = eurosport2.Schedule.FirstOrDefault(si => si.IsCurrentlyRunning);
                    if (item != null)
                        Console.WriteLine("Schedule item: " + item.Name + " from " + item.StartDate.DateTime + " to " +
                                          item.EndDate.DateTime);
                    var handler = eurosport2.LiveStreams[0].Stream();
                    if (!isMono)
                        handler.OnStreamData += RelayData;
                    if (first && !isMono && startVLC)
                    {
                        StartVLC();
                        first = false;
                    }
                    handler.Start();
                }
                catch (Exception)
                {
                    Console.WriteLine("Unhandled exception, restarting");
                }
            }
        }

        private static void StartVLC()
        {
            const string path = @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe";
            if (File.Exists(path))
                Process.Start(path, "http://localhost:" + _port);
        }

        private static void RelayData(object sender, StreamDataEvent e)
        {
            if (_relay != null)
                _relay.Relay(e);
        }
    }
}
