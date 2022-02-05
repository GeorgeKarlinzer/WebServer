using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameServer.Data
{
    public class GameLog
    {
        public int ID { get; set; }
        public string Log { get; set; }

        public GameLog(string log)
        {
            Log = log;
        }
    }
}
