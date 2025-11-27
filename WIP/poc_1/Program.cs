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
           

            var pluginPath = Path.Combine(AppContext.BaseDirectory, "Modules");
            foreach (var dll in Directory.GetFiles(pluginPath, "*.dll"))
            {
                var assembly = Assembly.LoadFrom(dll);
                var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in moduleTypes)
                {
                    var module = (IModule)Activator.CreateInstance(type)!;
                    Console.WriteLine($"Loaded module: {module.Name}");
                    module.Execute();
                }
            }

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
