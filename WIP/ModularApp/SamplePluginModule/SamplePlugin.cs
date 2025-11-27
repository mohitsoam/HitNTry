using System;
using System.Collections.Generic;
using System.Text;

namespace SamplePluginModule
{
    //using Azure.Messaging.ServiceBus;
    //using Confluent.Kafka;
    using MainHostApp;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using PluginContracts;
    //using PluginContracts;
    //using StackExchange.Redis;
    using System.Data;

    public class SamplePlugin : IModule
    {
        private IConfiguration? _config;
        //private ServiceBusClient? _sbClient;
        private IDbConnection? _db;
        //private IProducer<Null, string>? _kafka;
        //private IConnectionMultiplexer? _redis;
        private ILogger<SamplePlugin>? _logger;
        private ModularDbContext? _dbContext;

        public string Name => "SamplePlugin";

        public void Initialize(IServiceProvider serviceProvider)
        {
            _config = serviceProvider.GetService<IConfiguration>();
            //_sbClient = serviceProvider.GetService<ServiceBusClient>();
            _db = serviceProvider.GetService<IDbConnection>();
            //_kafka = serviceProvider.GetService<IProducer<Null, string>>();
            //_redis = serviceProvider.GetService<IConnectionMultiplexer>();
            _logger = serviceProvider.GetService<ILogger<SamplePlugin>>();
            _dbContext = serviceProvider.GetService<ModularDbContext>();
        }

        public void Execute()
        {
            _logger?.LogInformation("Executing SamplePlugin...");

            //_kafka?.Produce("my-topic", new Message<Null, string> { Value = "Hello from plugin" });

            //var redisDb = _redis?.GetDatabase();
            //redisDb?.StringSet("plugin:status", "active");

            _db?.Open();
            using var cmd = _db?.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users";
            var count = cmd.ExecuteScalar();
            Console.WriteLine($"[SamplePlugin] User count: {count}");
            _db?.Close();

            //var users = _dbContext?.Users.ToList();
            //Console.WriteLine($"[SamplePlugin] EF Users: {users?.Count}");

            Console.WriteLine("Kafka, Redis, DB, and EF Core operations completed.");
        }
    }
}
