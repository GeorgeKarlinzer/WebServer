using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameServer.Data
{
    public class Word
    {
        public Word(string Value)
        {
            this.Value = Value;
        }

        public int ID { get; set; }
        public string Value { get; set; }
    }
}
