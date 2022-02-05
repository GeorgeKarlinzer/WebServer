using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace GameServer
{
    public static class ExtensionsMethods
    {
        public static string ListToString(this List<TcpClient> clients, Dictionary<TcpClient, string> loginsMap) =>
            ListToString(clients, loginsMap, 0);

        public static string ListToString(this List<TcpClient> clients, Dictionary<TcpClient, string> loginsMap, int skipAmount)
        {
            return string.Join(", ", loginsMap
                                    .Where((k) => clients.Skip(Math.Clamp(skipAmount, 0, clients.Count))
                                                  .Contains(k.Key))
                                    .Select((k) => k.Value));
        }
    }
}