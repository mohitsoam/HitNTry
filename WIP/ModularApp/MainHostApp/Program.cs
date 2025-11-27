using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.Sqlite;

namespace MainHostApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // build host
            var host = CreateHostBuilder(args).Build();

            // build plugin service provider (shared config/db)
            var serviceProvider = ServiceProviderBuilder.Build();

            // Load plugins BEFORE running the web host
            PluginLoader.LoadAndExecuteModules(serviceProvider);

            // now run web host (blocks)
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
