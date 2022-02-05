using GameServer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using static GameServer.LogsHandler;
using System.IO;

namespace GameServer
{
    public class Server
    {
        private readonly int maxPlayers;
        public readonly int minPlayers = 2;

        private TcpListener tcpServer;

        private Dictionary<TcpClient, Queue<string>> bufferQueuesMap;

        public Dictionary<TcpClient, string> loginsMap;

        private int waitForGame;

        private Queue<TcpClient> clientQueue = new();

        private object disconnectLocker = new();
        private object authLocker = new();

        private bool gameStarted = true;


        public Server(int maxPlayers, int minPlayers, int waitForGame)
        {
            this.maxPlayers = maxPlayers;
            this.minPlayers = minPlayers;
            this.waitForGame = waitForGame;
        }

        public bool TrySend(TcpClient client, string message)
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(message + "\n");

                var stream = client.GetStream();
                stream.Write(data, 0, data.Length);

                return true;
            }
            catch
            {
                if (client.Connected)
                    throw;

                return false;
            }
        }

        public string Recieve(TcpClient client)
        {
            try
            {
                if (bufferQueuesMap[client].Count > 0)
                    return bufferQueuesMap[client].Dequeue();

                var buffer = new byte[255];
                int len = client.GetStream().Read(buffer, 0, buffer.Length);

                var str = Encoding.UTF8.GetString(buffer, 0, len);

                var separator = str.Contains('\n') ? '\n' : '\0';

                foreach (var s in str.Split(separator))
                    if (s != "")
                        bufferQueuesMap[client].Enqueue(s);

                if (!bufferQueuesMap.ContainsKey(client) || bufferQueuesMap[client].Count == 0)
                    return "-";

                return bufferQueuesMap[client].Dequeue();
            }
            catch
            {
                return "-";
            }
        }

        private void BeforeGameInitialization()
        {
            WriteToLog(0, "Waiting for the players");
        }

        private void AddClientToQueue(TcpClient client)
        {
            lock (authLocker)
            {
                clientQueue.Enqueue(client);

                if (clientQueue.Count == minPlayers)
                {
                    Task.Run(() =>
                    {
                        gameStarted = false;

                        var timeNow = DateTime.Now;
                        while((DateTime.Now - timeNow).TotalMilliseconds < waitForGame)
                        {
                            Thread.Sleep(500);
                            if (gameStarted)
                                return;
                        }

                        if (clientQueue.Count >= minPlayers && !gameStarted)
                            StartGame();
                    });
                }

                if (clientQueue.Count == maxPlayers)
                    StartGame();
            }
        }

        private void StartGame()
        {
            gameStarted = true;
            var clients = new List<TcpClient>();
            while (clients.Count < maxPlayers && clientQueue.Count > 0)
                clients.Add(clientQueue.Dequeue());

            var gameId = GetNewGameID();
            var game = new GameSession(clients, this, gameId);
            new Thread(() => game.Start()).Start();
            BeforeGameInitialization();
        }

        private void RemoveClientFromQueue(TcpClient client)
        {
            lock (authLocker)
            {
                var bufferQueue = new Queue<TcpClient>();
                while (clientQueue.Count > 0)
                {
                    bufferQueue.Enqueue(clientQueue.Dequeue());

                    if (bufferQueue.Peek() == client)
                    {
                        bufferQueue.Dequeue();
                        while (bufferQueue.Count > 0)
                            clientQueue.Enqueue(bufferQueue.Dequeue());
                        return;
                    }
                }
            }
        }

        private async void HandleConnectionAsync(TcpClient client)
        {
            WriteToLog(0, $"{client.Client.RemoteEndPoint} connected");

            var auth = false;
            lock (authLocker)
            {
                bufferQueuesMap.Add(client, new Queue<string>());
            }

            await Task.Run(() => auth = TryAuth(client));

            TrySend(client, auth ? "+2" : "-");
            if (!auth)
            { DisconnectClient(null, client, DisconnectCause.BadAuth); return; }

            AddClientToQueue(client);
        }

        public bool TryAuth(TcpClient client)
        {
            lock (authLocker)
            {
                if (loginsMap.ContainsKey(client))
                    return false;

                loginsMap.Add(client, client.Client.RemoteEndPoint.ToString());
            }

            var login = Recieve(client);
            if (login == "-")
                return false;

            var password = Recieve(client);
            if (password == "-")
                return false;

            lock (authLocker)
            {
                if (loginsMap.ContainsValue(login))
                {
                    var c = loginsMap
                        .ToList()
                        .First(l => l.Value == login)
                        .Key;

                    try
                    {
                        var data = new byte[0];

                        var stream = client.GetStream();
                        Console.WriteLine(stream.DataAvailable);
                        return false;
                    }
                    catch
                    {
                        DisconnectClient(null, c, DisconnectCause.ConnectionError);
                        RemoveClientFromQueue(c);
                    }
                }

                using var db = new ServerDbContext();
                var user = db.AppUsers.FirstOrDefault(u => u.Login == login);

                if (user == default)
                    return false;

                if (password != user.Password || loginsMap.ContainsValue(login))
                    return false;

                if (db.AppUsers.First(u => u.Login == login).GamesAmount >= 30)
                    return false;

                WriteToLog(0, $"{client.Client.RemoteEndPoint} logged in with login: {login}");
                loginsMap[client] = login;
            }

            return true;
        }

        public void Run(IPAddress ipAd, int port)
        {
            loginsMap = new();
            bufferQueuesMap = new();
            tcpServer = new TcpListener(ipAd, port);
            tcpServer.Start();

            WriteToLog(0, $"Server is running on {ipAd}:{port}");

            BeforeGameInitialization();

            while (true)
            {
                try
                {
                    HandleConnectionAsync(tcpServer.AcceptTcpClient());
                }
                catch (Exception e)
                {
                    WriteToLog(0, $"Error: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        public void DisconnectClient(GameSession session, TcpClient client, DisconnectCause cause)
        {
            lock (disconnectLocker)
            {
                if (!loginsMap.ContainsKey(client))
                    return;

                if (session != null)
                {
                    WriteToLog(session.gameId, $"{loginsMap[client]} has been disconnected, because of '{cause}'");

                    WriteToLog(0, $"{loginsMap[client]} (game {session.gameId}) has been disconnected, because of '{cause}'");
                }
                else
                    WriteToLog(0, $"{loginsMap[client]} has been disconnected, because of '{cause}'");


                if (client.Connected)
                {
                    TrySend(client, "?");
                    client.Close();
                }

                session?.clients?.Remove(client);
                if (session != null && session.clients.Count == 0)
                {
                    WriteToLog(0, $"Game {session.gameId} has been finished");
                    WriteToLog(session.gameId, $"Game has been finished");
                }

                bufferQueuesMap.Remove(client);
                loginsMap.Remove(client);
            }
        }
    }
}
