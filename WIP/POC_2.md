Here's the **full production-ready modular plugin framework** with:

- 🔌 Dynamic plugin loading via reflection  
- 🧩 Loose coupling (no direct references)  
- 🛠️ Dependency injection of config, Service Bus, DB, Kafka, Redis  
- 🧪 Sample plugin using all services  
- 🔄 Ready for hot-reload via `AssemblyLoadContext`  
- 🧬 Scoped EF Core `DbContext` injection (Blazor-compatible)  
- 🧪 Unit test scaffolding with mock `IServiceProvider`  

---

## 🧱 Solution Layout

```
ModularApp/
├── PluginContracts/              # Shared interface
│   └── IModule.cs
│
├── MainHostApp/                  # Host app
│   ├── Modules/                  # Drop plugin DLLs here
│   ├── Program.cs
│   ├── ServiceProviderBuilder.cs
│   ├── PluginLoader.cs
│   ├── appsettings.json
│   └── ModularDbContext.cs       # Optional EF Core DbContext
│
└── SamplePluginModule/           # Example plugin
    └── SamplePlugin.cs
```

---

## 🔹 PluginContracts/IModule.cs

```csharp
namespace PluginContracts;

public interface IModule
{
    string Name { get; }
    void Initialize(IServiceProvider serviceProvider); // Inject config, SB, DB, Kafka, Redis
    void Execute(); // Host-invoked logic
}
```

---

## 🔹 MainHostApp

### 📄 Program.cs

```csharp
using PluginContracts;

var serviceProvider = ServiceProviderBuilder.Build();
PluginLoader.LoadAndExecuteModules(serviceProvider);
```

---

### 📄 PluginLoader.cs

```csharp
using PluginContracts;
using System.Reflection;
using System.Runtime.Loader;

public static class PluginLoader
{
    public static void LoadAndExecuteModules(IServiceProvider serviceProvider)
    {
        var pluginPath = Path.Combine(AppContext.BaseDirectory, "Modules");
        foreach (var dll in Directory.GetFiles(pluginPath, "*.dll"))
        {
            var context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(dll), isCollectible: true);
            using var stream = new FileStream(dll, FileMode.Open, FileAccess.Read);
            var assembly = context.LoadFromStream(stream);

            var moduleTypes = assembly.GetTypes()
                .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in moduleTypes)
            {
                var module = (IModule)Activator.CreateInstance(type)!;
                module.Initialize(serviceProvider);
                Console.WriteLine($"Executing module: {module.Name}");
                module.Execute();
            }

            context.Unload(); // Optional: unload after execution
        }
    }
}
```

---

### 📄 ServiceProviderBuilder.cs

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Azure.Messaging.ServiceBus;
using Confluent.Kafka;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.SqlClient;

public static class ServiceProviderBuilder
{
    public static IServiceProvider Build()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(config);

        services.AddSingleton<ServiceBusClient>(sp =>
        {
            var connStr = config["ServiceBus:ConnectionString"];
            return new ServiceBusClient(connStr);
        });

        services.AddScoped<IDbConnection>(sp =>
        {
            var dbConnStr = config["Database:ConnectionString"];
            return new SqlConnection(dbConnStr);
        });

        services.AddSingleton<IProducer<Null, string>>(sp =>
        {
            var kafkaConfig = new ProducerConfig
            {
                BootstrapServers = config["Kafka:BootstrapServers"]
            };
            return new ProducerBuilder<Null, string>(kafkaConfig).Build();
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisConnStr = config["Redis:ConnectionString"];
            return ConnectionMultiplexer.Connect(redisConnStr);
        });

        services.AddDbContext<ModularDbContext>(options =>
        {
            options.UseSqlServer(config["Database:ConnectionString"]);
        });

        services.AddLogging();

        return services.BuildServiceProvider();
    }
}
```

---

### 📄 ModularDbContext.cs (Optional EF Core)

```csharp
using Microsoft.EntityFrameworkCore;

public class ModularDbContext : DbContext
{
    public ModularDbContext(DbContextOptions<ModularDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
```

---

### 📄 appsettings.json

```json
{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://your-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=..."
  },
  "Database": {
    "ConnectionString": "Server=your-db-server;Database=your-db;User Id=your-user;Password=your-password;"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

---

## 🔹 SamplePluginModule/SamplePlugin.cs

```csharp
using PluginContracts;
using Azure.Messaging.ServiceBus;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Data;
using Microsoft.EntityFrameworkCore;

public class SamplePlugin : IModule
{
    private IConfiguration? _config;
    private ServiceBusClient? _sbClient;
    private IDbConnection? _db;
    private IProducer<Null, string>? _kafka;
    private IConnectionMultiplexer? _redis;
    private ILogger<SamplePlugin>? _logger;
    private ModularDbContext? _dbContext;

    public string Name => "SamplePlugin";

    public void Initialize(IServiceProvider serviceProvider)
    {
        _config = serviceProvider.GetService<IConfiguration>();
        _sbClient = serviceProvider.GetService<ServiceBusClient>();
        _db = serviceProvider.GetService<IDbConnection>();
        _kafka = serviceProvider.GetService<IProducer<Null, string>>();
        _redis = serviceProvider.GetService<IConnectionMultiplexer>();
        _logger = serviceProvider.GetService<ILogger<SamplePlugin>>();
        _dbContext = serviceProvider.GetService<ModularDbContext>();
    }

    public void Execute()
    {
        _logger?.LogInformation("Executing SamplePlugin...");

        _kafka?.Produce("my-topic", new Message<Null, string> { Value = "Hello from plugin" });

        var redisDb = _redis?.GetDatabase();
        redisDb?.StringSet("plugin:status", "active");

        _db?.Open();
        using var cmd = _db?.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Users";
        var count = cmd.ExecuteScalar();
        Console.WriteLine($"[SamplePlugin] User count: {count}");
        _db?.Close();

        var users = _dbContext?.Users.ToList();
        Console.WriteLine($"[SamplePlugin] EF Users: {users?.Count}");

        Console.WriteLine("Kafka, Redis, DB, and EF Core operations completed.");
    }
}
```

---

## 🧪 Unit Test Scaffolding

```csharp
public class SamplePluginTests
{
    [Fact]
    public void Execute_ShouldRunWithoutError()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IProducer<Null, string>>(new Mock<IProducer<Null, string>>().Object);
        services.AddSingleton<IConnectionMultiplexer>(Mock.Of<IConnectionMultiplexer>());
        services.AddScoped<IDbConnection>(_ => new SqlConnection("FakeConnection"));
        services.AddDbContext<ModularDbContext>(options => options.UseInMemoryDatabase("TestDb"));

        var provider = services.BuildServiceProvider();

        var plugin = new SamplePlugin();
        plugin.Initialize(provider);
        plugin.Execute();
    }
}
```

---

Would you like to add:
- 🔍 Plugin metadata discovery via `[ModuleInfo]` attributes?
- 🧠 AI-assisted plugin registration or orchestration?
- 📦 Plugin packaging and versioning via NuGet or zip bundles?

Let’s keep scaling this into a full plugin ecosystem.