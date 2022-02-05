using GameServer.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace GameServer
{
    public static class LogsHandler
    {
        private static object locker = new();

        public enum DisconnectCause
        {
            ConnectionError,
            BadWord,
            WonGame,
            LostGame,
            BadAuth,
            AlreadyLoggedIn,
            AuthTimedOut,
            GuessTimedOut,
            SetWordTimedOut,
            SetWord,
            StopGame,
            WordGuessed
        }

        public static int GetNewGameID()
        {
            using var db = new ServerDbContext();

            var newLog = new GameLog("");

            db.GameLogs.Add(newLog);
            db.SaveChanges();

            return newLog.ID;
        }

        public static void WriteToLog(int gameId, string message)
        {
            lock (locker)
            {
                var time = DateTime.Now.ToString();
                string m = $"{time}:\t{message}\n";
                if (gameId == 0)
                {
                    var path = "";
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        path = @"C:\Users\Legion\source\repos\GameServer\GameServer\globalLog.txt";
                    else
                        path = "/home/admin/ubuntu.20.04-x64/globalLog.txt";

                    if (!File.Exists(path))
                        File.Create(path);

                    using (var sw = File.AppendText(path))
                        sw.Write(m);
                    Console.WriteLine(m);

                    return;
                }


                using var db = new ServerDbContext();

                var log = db.GameLogs.ToList().Where(x => x.ID == gameId).First();

                log.Log += m;

                db.SaveChanges();
            }
        }
    }
}
