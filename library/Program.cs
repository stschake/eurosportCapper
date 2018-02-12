using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using esnew.model;
using System.Linq;

namespace esnew
{
    class Program
    {
        private static async Task Play(EurosportPlayer player, Airing airing)
        {
            var handler = await player.GetStream(airing.BestPlaybackUrl);
            handler.NewVideoSequence += async (source, part) => 
            { 
                await File.WriteAllBytesAsync(string.Format("{0:00000}.ts", part.SequenceNumber), part.RawData);
                Console.WriteLine($"Downloaded {part.SequenceNumber}, {part.PartsInQueue} still queued");
            };

            await handler.Start();
        }

        static async Task MainAsync()
        {
            // This stores the GUID, picking a random one on every start probably isn't a good idea
            Settings.Initialize();

            var player = new EurosportPlayer();
            // Country doesn't need to match your IP or location
            var res = await player.Login("your@email.com", "yourPassword", Country.UnitedKingdom);
            if (!res)
            {
                Console.WriteLine("Failed to login, check your credentials");
                return;
            }

            // OnDemand has all the past videos and streams
            var onDemand = await player.GetAllOnDemand();
            var dubai = onDemand.Airings.Where(airing => airing.EnglishTitle.Title.Contains("Dubai"));
            await Play(player, dubai.Skip(5).First());

            // AiringsOnNow are the current live programs, one Airing for each channel 
            /*var onNow = await player.GetAiringsOnNow();
            await Play(player, onNow.First());*/
        }

        static void Main(string[] args)
        {
            MainAsync().Wait();
        }
    }
}
