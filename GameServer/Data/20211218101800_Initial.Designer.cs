﻿// <auto-generated />
using GameServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GameServer.Data
{
    [DbContext(typeof(ServerDbContext))]
    [Migration("20211218101800_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.13");

            modelBuilder.Entity("GameServer.Data.AppUser", b =>
                {
                    b.Property<string>("Login")
                        .HasColumnType("TEXT");

                    b.Property<int>("GamesAmount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("TEXT");

                    b.Property<int>("Score")
                        .HasColumnType("INTEGER");

                    b.HasKey("Login");

                    b.ToTable("AppUsers");

                    b.HasData(
                        new
                        {
                            Login = "409165",
                            GamesAmount = 0,
                            PasswordHash = "A665A45920422F9D417E4867EFDC4FB8A04A1F3FFF1FA07E998E86F7F7A27AE3",
                            Score = 0
                        },
                        new
                        {
                            Login = "409166",
                            GamesAmount = 0,
                            PasswordHash = "A665A45920422F9D417E4867EFDC4FB8A04A1F3FFF1FA07E998E86F7F7A27AE3",
                            Score = 0
                        },
                        new
                        {
                            Login = "409167",
                            GamesAmount = 0,
                            PasswordHash = "A665A45920422F9D417E4867EFDC4FB8A04A1F3FFF1FA07E998E86F7F7A27AE3",
                            Score = 0
                        });
                });

            modelBuilder.Entity("GameServer.Data.GameLog", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Log")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.HasIndex("ID");

                    b.ToTable("GameLogs");
                });

            modelBuilder.Entity("GameServer.Data.Word", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.HasIndex("ID");

                    b.ToTable("Words");
                });
#pragma warning restore 612, 618
        }
    }
}
