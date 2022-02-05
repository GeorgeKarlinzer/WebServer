using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GameServer.Data;
using static GameServer.LogsHandler;

namespace GameServer
{
    public class GameSession
    {
        private readonly int maxTries = 10;

        private readonly int waitForGuessDisconnectTime = 10 * 1000;
        private readonly int waitForGuessIgnoreTime = 2 * 1000;

        public readonly int gameId;

        public List<TcpClient> clients;

        private string word;
        private string codedWord;

        private readonly Server server;

        private bool isGuessed;


        public GameSession(List<TcpClient> clients, Server server, int gameId)
        {
            this.gameId = gameId;
            this.clients = clients;
            this.server = server;
            this.word = "-";
        }

        public async void StartAsync()
        {
            await Task.Run(() => Start());
        }

        public void Start()
        {
            var logins = server.loginsMap
                .Where(login => clients.Contains(login.Key))
                .Select(pair => pair.Value);

            using var db = new ServerDbContext();

            db.AppUsers
                .Where(user => logins.Contains(user.Login))
                .ToList()
                .ForEach(u => u.GamesAmount++);

            db.SaveChanges();

            GetSessionWord();
            codedWord = GetCodedWord();

            if (word == "-")
            {
                Stop();
                return;
            }

            WriteToLog(gameId, $"Game starts. Word: {word}\tPlayers: {clients.ListToString(server.loginsMap)}");

            WriteToLog(0, $"Game starts. Word: {word}\tPlayers: {clients.ListToString(server.loginsMap)}\tGameID: {gameId}");

            RunClientsSession();
            //foreach (var c in clients)
            //new Thread(() => RunClientSession(c)).Start();
        }

        public void Stop()
        {
            while (clients.Count > 0)
                server.DisconnectClient(this, clients[0], DisconnectCause.StopGame);
        }

        private void GetSessionWord()
        {
            var lMap = server.loginsMap;

            while (word.Length < 5)
            {
                using var db = new ServerDbContext();

                var randIndex = new Random().Next(0, db.Words.Count());

                word = db.Words.First(w => w.ID == randIndex).Value;
            }
        }

        private string GetCodedWord()
        {
            var coded = "";

            foreach (var c in word)
                coded += "acemnorsuwzxv".Contains(c) ? 1 :
                    "ąęgjpyq".Contains(c) ? 2 :
                    "bćdhklłńóśtźżi".Contains(c) ? 3 : 4;

            return coded;
        }

        private void RunClientsSession()
        {
            var deletedQueue = new Queue<(TcpClient, DisconnectCause)>();

            var fullClientMap = new Dictionary<TcpClient, Client>();

            foreach (var c in clients)
            {
                fullClientMap.Add(c, new(0, false, new()));
                fullClientMap[c].Score = 0;

                if (!server.TrySend(c, codedWord))
                    deletedQueue.Enqueue((c, DisconnectCause.ConnectionError));
            }

            HandleDeletedClients();

            for (int i = 0; i < maxTries && clients.Count > 0; i++)
            {
                bool isGuessed = this.isGuessed;

                foreach (var client in clients)
                {
                    var beforeResponseTime = DateTime.Now;
                    var cts = new CancellationTokenSource();

                    var arg = "";
                    var guess = "";
                    var userInput = Task.Run(() =>
                    {
                        arg = server.Recieve(client);
                        guess = server.Recieve(client);
                    }, cts.Token);

                    while (!userInput.IsCompleted && (DateTime.Now - beforeResponseTime).TotalMilliseconds < waitForGuessDisconnectTime)
                        Thread.Sleep(50);

                    var isIgnore = false;

                    if ((DateTime.Now - beforeResponseTime).TotalMilliseconds >= waitForGuessDisconnectTime)
                    {
                        cts.Cancel();
                        deletedQueue.Enqueue((client, DisconnectCause.GuessTimedOut));
                        continue;
                    }

                    if ((DateTime.Now - beforeResponseTime).TotalMilliseconds >= waitForGuessIgnoreTime)
                        isIgnore = true;


                    // "-" means error while recieving
                    if (arg == "-" || guess == "-")
                    { deletedQueue.Enqueue((client, DisconnectCause.ConnectionError)); continue; }

                    // Handle ignore cases
                    if (isIgnore || (arg != "+" && arg != "=")
                        || (arg == "+" && guess.Length != 1) || fullClientMap[client].guessedLetters.Contains(guess))
                    {
                        if (!server.TrySend(client, "#"))
                        { deletedQueue.Enqueue((client, DisconnectCause.ConnectionError)); continue; }

                        var cause = isIgnore ? "timed out" :
                            !fullClientMap[client].guessedLetters.Contains(guess) ? "incorrect input" : $"the letter has already been guessed";

                        WriteToLog(gameId, $"{server.loginsMap[client]}'s response '{guess}' was ignored ({cause})");

                        continue;
                    }

                    // Handle case, when the word has been already guessed
                    if (isGuessed)
                    {
                        EndGame(client, fullClientMap[client].Score, fullClientMap[client].IsWon);
                        deletedQueue.Enqueue((client, DisconnectCause.WordGuessed));
                        continue;
                    }

                    // Handle guess word try
                    if (arg == "=")
                    {
                        if (guess != word)
                        {
                            if (!server.TrySend(client, "!"))
                            { deletedQueue.Enqueue((client, DisconnectCause.ConnectionError)); continue; }

                            WriteToLog(gameId, $"{server.loginsMap[client]}'s suggested word was '{guess}' - wrong");

                            continue;
                        }

                        if (!server.TrySend(client, "="))
                        { deletedQueue.Enqueue((client, DisconnectCause.ConnectionError)); continue; }

                        fullClientMap[client].Score += 5;
                        fullClientMap[client].IsWon = true;

                        WriteToLog(gameId, $"{server.loginsMap[client]}'s suggested word was '{guess}' - correct");
                        EndGame(client, fullClientMap[client].Score, fullClientMap[client].IsWon);
                        deletedQueue.Enqueue((client, DisconnectCause.WonGame));

                        continue;
                    }

                    // Handle guess letter try
                    var response = "";
                    var matches = Regex.Matches(word, $@"{guess}");
                    fullClientMap[client].Score += matches.Count;


                    for (int k = 0; k < word.Length; k++)
                        response += matches.Select(m => m.Index).Contains(k) ? "1" : "0";


                    if (matches.Count > 0)
                    {
                        fullClientMap[client].guessedLetters.Add(guess);
                        WriteToLog(gameId, $"{server.loginsMap[client]}'s suggested letter was '{guess}' - correct x{matches.Count}");

                        if (!server.TrySend(client, "=") || !server.TrySend(client, response))
                        { deletedQueue.Enqueue((client, DisconnectCause.ConnectionError)); continue; }

                        if (fullClientMap[client].Score == word.Length)
                        {
                            fullClientMap[client].IsWon = true;

                            continue;
                        }
                        continue;
                    }


                    WriteToLog(gameId, $"{server.loginsMap[client]}'s suggested letter was '{guess}' - incorrect");

                    if (!server.TrySend(client, "!"))
                    { deletedQueue.Enqueue((client, DisconnectCause.ConnectionError)); continue; }
                }

                HandleDeletedClients();
            }

            while (clients.Count > 0)
            {
                var c = clients[0];
                var fc = fullClientMap[c];

                EndGame(c, fc.Score, fc.IsWon);
                server.DisconnectClient(this, c, fc.IsWon ? DisconnectCause.WonGame : DisconnectCause.LostGame);
            }

            void HandleDeletedClients()
            {
                while (deletedQueue.Count > 0)
                {
                    var deletedClient = deletedQueue.Dequeue();
                    fullClientMap.Remove(deletedClient.Item1);

                    server.DisconnectClient(this, deletedClient.Item1, deletedClient.Item2);
                }
            }
        }



        //private void RunClientSession(TcpClient client)
        //{
        //    Thread.Sleep(100);

        //    if (!server.TrySend(client, codedWord))
        //    { server.DisconnectClient(this, client, DisconnectCause.ConnectionError); return; }

        //    var score = 0;
        //    var isWon = false;
        //    var guessedLetters = new List<string>();


        //    for (int i = 0; i < maxTries && !isWon; i++)
        //    {
        //        var beforeResponseTime = DateTime.Now;
        //        var cts = new CancellationTokenSource();

        //        var arg = "";
        //        var guess = "";
        //        var userInput = Task.Run(() =>
        //        {
        //            arg = server.Recieve(client);
        //            guess = server.Recieve(client);
        //        }, cts.Token);

        //        while (!userInput.IsCompleted && (DateTime.Now - beforeResponseTime).TotalMilliseconds < waitForGuessDisconnectTime)
        //        {
        //            Thread.Sleep(100);
        //        }

        //        var isIgnore = false;

        //        if ((DateTime.Now - beforeResponseTime).TotalMilliseconds >= waitForGuessDisconnectTime)
        //        {
        //            cts.Cancel();
        //            server.DisconnectClient(this, client, DisconnectCause.GuessTimedOut);
        //            return;
        //        }

        //        if ((DateTime.Now - beforeResponseTime).TotalMilliseconds >= waitForGuessIgnoreTime)
        //            isIgnore = true;


        //        // "-" means error while recieving
        //        if (arg == "-" || guess == "-")
        //        { server.DisconnectClient(this, client, DisconnectCause.ConnectionError); return; }

        //        // Handle ignore cases
        //        if (isIgnore || (arg != "+" && arg != "=")
        //            || (arg == "+" && guess.Length != 1) || guessedLetters.Contains(guess))
        //        {
        //            if (!server.TrySend(client, "#"))
        //            { server.DisconnectClient(this, client, DisconnectCause.ConnectionError); return; }

        //            var cause = isIgnore ? "timed out" :
        //                !guessedLetters.Contains(guess) ? "incorrect input" : $"the letter has already been guessed";

        //            WriteToLog(gameId, $"{server.loginsMap[client]}'s response '{guess}' was ignored ({cause})");

        //            continue;
        //        }

        //        // Handle case, when the word has been already guessed
        //        if (isGuessed)
        //        {
        //            EndGame(client, DisconnectCause.WordGuessed, score, isWon);
        //            return;
        //        }

        //        // Handle guess word try
        //        if (arg == "=")
        //        {
        //            if (guess != word)
        //            {
        //                if (!server.TrySend(client, "!"))
        //                { server.DisconnectClient(this, client, DisconnectCause.ConnectionError); return; }

        //                WriteToLog(gameId, $"{server.loginsMap[client]}'s suggested word was '{guess}' - wrong");

        //                continue;
        //            }

        //            if (!server.TrySend(client, "="))
        //            { server.DisconnectClient(this, client, DisconnectCause.ConnectionError); return; }

        //            score += 5;
        //            isWon = true;

        //            isGuessed = true;
        //            WriteToLog(gameId, $"{server.loginsMap[client]}'s suggested word was '{guess}' - correct");

        //            break;
        //        }

        //        // Handle guess letter try
        //        var response = "";
        //        var matches = Regex.Matches(word, $@"{guess}");
        //        score += matches.Count;


        //        for (int k = 0; k < word.Length; k++)
        //            response += matches.Select(m => m.Index).Contains(k) ? "1" : "0";


        //        if (matches.Count > 0)
        //        {
        //            guessedLetters.Add(guess);
        //            WriteToLog(gameId, $"{server.loginsMap[client]}'s suggested letter was '{guess}' - correct x{matches.Count}");

        //            if (!server.TrySend(client, "=") || !server.TrySend(client, response))
        //            { server.DisconnectClient(this, client, DisconnectCause.ConnectionError); return; }

        //            if (score == word.Length)
        //            {
        //                isWon = true;
        //                break;
        //            }
        //            continue;
        //        }


        //        WriteToLog(gameId, $"{server.loginsMap[client]}'s suggested letter was '{guess}' - incorrect");

        //        if (!server.TrySend(client, "!"))
        //        { server.DisconnectClient(this, client, DisconnectCause.ConnectionError); return; }
        //    }

        //    // On finish
        //    EndGame(client, isWon ? DisconnectCause.WonGame : DisconnectCause.LostGame, score, isWon);
        //}

        private void EndGame(TcpClient client, int score, bool isWon)
        {
            using var db = new ServerDbContext();
            db.AppUsers
                .First(user => user.Login == server.loginsMap[client])
                .Score += score;
            db.SaveChanges();

            server.TrySend(client, score.ToString());
            if (!isGuessed && isWon)
                isGuessed = true;

            WriteToLog(gameId, $"{server.loginsMap[client]} " + (isWon ? "won" : "lost") + $" and got {score} points");
            //server.DisconnectClient(this, client, cause);
        }

        public async Task<string> GetWordAsync(TcpClient client, CancellationToken token)
        {
            if (!server.TrySend(client, "@"))
                return "-";

            var word = "-";
            await Task.Run(() => word = server.Recieve(client));

            using var db = new ServerDbContext();
            var isExist = db.Words
                .Any(w => w.Value == word);

            return isExist ? word : "-";
        }
    }
}
