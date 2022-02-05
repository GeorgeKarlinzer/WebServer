namespace GameServer.Data
{
    public class AppUser
    {
        public AppUser(string login, string password, int score = 0, int gamesAmount = 0)
        {
            Login = login;
            Password = password;
            Score = score;
            GamesAmount = gamesAmount;
        }

        public string Login { get; set; }
        public string Password { get; set; }
        public int Score { get; set; }
        public int GamesAmount { get; set; }
    }
}
