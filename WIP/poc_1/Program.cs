using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace poc_1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Build host FIRST (so DI is available)
            var host = CreateHostBuilder(args).Build();

            // Access DI service provider
            var serviceProvider = host.Services;

            // Load and run plugins
            LoadPlugins(serviceProvider);

            // Now run the Blazor Server host
            host.Run();
        }

        private static void LoadPlugins(IServiceProvider serviceProvider)
        {
            var pluginPath = Path.Combine(AppContext.BaseDirectory, "Modules");

            if (!Directory.Exists(pluginPath))
            {
                Console.WriteLine("No plugin folder found.");
                return;
            }

            foreach (var dll in Directory.GetFiles(pluginPath, "*.dll"))
            {
                var assembly = Assembly.LoadFrom(dll);

                var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(IModuleDb).IsAssignableFrom(t)
                                && !t.IsInterface && !t.IsAbstract);

                foreach (var type in moduleTypes)
                {
                    var module = (IModuleDb)Activator.CreateInstance(type)!;

                    Console.WriteLine($"Loaded module: {module.Name}");

                    // 🔥 IMPORTANT: Initialize with DI Services
                    module.Initialize(serviceProvider);

                    // Execute plugin
                    module.Execute();
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
