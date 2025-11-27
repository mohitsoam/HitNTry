using PluginContracts;
using System;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace HelloWorldModule
{
    public class DbModule : IModuleDb
    {
        private IConfiguration? _config;
        private IDbConnection? _db;

        public string Name => "DbModule";

        public void Initialize(IServiceProvider serviceProvider)
        {
            _config = serviceProvider.GetService(typeof(IConfiguration)) as IConfiguration;
            _db = serviceProvider.GetService(typeof(IDbConnection)) as IDbConnection;
        }

        public void Execute()
        {
            Console.WriteLine($"DB Connection: {_db?.ConnectionString}");
            //Console.WriteLine($"SB Topic: {_config?["ServiceBus:Topic"]}");

            if (_db == null)
            {
                Console.WriteLine("ERROR: No database connection resolved.");
                return;
            }

            try
            {
                _db.Open();

                using var cmd = _db.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM Users";

                var result = cmd.ExecuteScalar();
                Console.WriteLine($"User count: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database error: " + ex.Message);
            }
            finally
            {
                if (_db.State != ConnectionState.Closed)
                {
                    _db.Close();
                }
            }
        }
    }
}
