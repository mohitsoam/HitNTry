using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SQLitePCL;
using System.Data;

namespace MainHostApp
{
    public static class ServiceProviderBuilder
    {
        public static IServiceProvider Build()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(config);

            // --- CREATE SINGLE SQLITE IN-MEMORY CONNECTION ---
            //var sqliteConnection = new SqliteConnection("DataSource=:memory:");
            //sqliteConnection.Open(); // MUST stay open entire app lifetime

            //// Register the SAME connection for all consumers (plugins etc.)
            //services.AddSingleton(sqliteConnection);
            //services.AddSingleton<IDbConnection>(sp => sqliteConnection);

            Batteries.Init(); // ← IMPORTANT: add this FIRST

            services.AddSingleton<IDbConnection>(sp =>
            {
                // Shared in-memory SQLite DB
                var conn = new SqliteConnection("Data Source=:memory:;Cache=Shared");

                conn.Open(); // Must remain open to keep in-memory DB alive

                // Optional: Create sample schema & data
                using var cmd = conn.CreateCommand();
                cmd.CommandText =
                @"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL
        );

        INSERT INTO Users (Name) VALUES ('Alice'), ('Bob'), ('Charlie');
    ";
                cmd.ExecuteNonQuery();

                return conn;
            });

            // ---- EF CORE ----
            //services.AddDbContext<ModularDbContext>((sp, options) =>
            //{
            //    var conn = sp.GetRequiredService<SqliteConnection>();
            //    options.UseSqlite(conn);
            //});

            services.AddLogging();

            // Build provider
            var provider = services.BuildServiceProvider();

            // Ensure EF schema is created
            //var db = provider.GetRequiredService<ModularDbContext>();
            //db.Database.EnsureCreated();

            return provider;
        }
    }
}
