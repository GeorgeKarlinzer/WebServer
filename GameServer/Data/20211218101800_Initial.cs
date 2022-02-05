using Microsoft.EntityFrameworkCore.Migrations;

namespace GameServer.Data
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Login = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: true),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    GamesAmount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Login);
                });

            migrationBuilder.CreateTable(
                name: "GameLogs",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Log = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameLogs", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Words",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Words", x => x.ID);
                });

            migrationBuilder.InsertData(
                table: "AppUsers",
                columns: new[] { "Login", "GamesAmount", "PasswordHash", "Score" },
                values: new object[] { "409165", 0, "A665A45920422F9D417E4867EFDC4FB8A04A1F3FFF1FA07E998E86F7F7A27AE3", 0 });

            migrationBuilder.InsertData(
                table: "AppUsers",
                columns: new[] { "Login", "GamesAmount", "PasswordHash", "Score" },
                values: new object[] { "409166", 0, "A665A45920422F9D417E4867EFDC4FB8A04A1F3FFF1FA07E998E86F7F7A27AE3", 0 });

            migrationBuilder.InsertData(
                table: "AppUsers",
                columns: new[] { "Login", "GamesAmount", "PasswordHash", "Score" },
                values: new object[] { "409167", 0, "A665A45920422F9D417E4867EFDC4FB8A04A1F3FFF1FA07E998E86F7F7A27AE3", 0 });

            migrationBuilder.CreateIndex(
                name: "IX_GameLogs_ID",
                table: "GameLogs",
                column: "ID");

            migrationBuilder.CreateIndex(
                name: "IX_Words_ID",
                table: "Words",
                column: "ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "GameLogs");

            migrationBuilder.DropTable(
                name: "Words");
        }
    }
}
