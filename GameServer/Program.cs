using GameServer.Data;
using System;
using System.Linq;
using System.Net;
using System.Configuration;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace GameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var maxPlayers = int.Parse(ConfigurationManager.AppSettings.Get("maxPlayers"));
                var minPlayers = int.Parse(ConfigurationManager.AppSettings.Get("minPlayers"));
                var waitForGame = int.Parse(ConfigurationManager.AppSettings.Get("waitForGame"));
                var port = int.Parse(ConfigurationManager.AppSettings.Get("port"));


                if (args.Contains("--clear"))
                {
                    using var db = new ServerDbContext();
                    db.AppUsers
                        .ToList()
                        .ForEach(u => { u.Score = 0; u.GamesAmount = 0; });

                    db.GameLogs
                        .Where(l => l.ID != 0)
                        .ToList()
                        .ForEach(l => db.Entry(l).State = EntityState.Deleted);

                    db.SaveChanges();
                }

                var server = new Server(maxPlayers, minPlayers, waitForGame);
#if DEBUG
                var myIP = IPAddress.Parse("127.0.0.1");
                server.Run(myIP, port);
#else
                var myIP = IPAddress.Parse("31.172.70.25");
                server.Run(myIP, port);
#endif
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
