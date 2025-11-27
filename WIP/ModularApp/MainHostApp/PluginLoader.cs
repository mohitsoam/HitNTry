using PluginContracts;
using System.Reflection;
using System.Runtime.Loader;

namespace MainHostApp
{
    public static class PluginLoader
    {
        public static void LoadAndExecuteModules(IServiceProvider serviceProvider)
        {
            var pluginPath = Path.Combine(AppContext.BaseDirectory, "Modules");
            if (!Directory.Exists(pluginPath))
                return;

            foreach (var dll in Directory.GetFiles(pluginPath, "*.dll"))
            {
                var context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(dll), true);

                using var stream = new FileStream(dll, FileMode.Open, FileAccess.Read);
                var assembly = context.LoadFromStream(stream);

                var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(IModule).IsAssignableFrom(t) &&
                                !t.IsInterface && !t.IsAbstract);

                foreach (var type in moduleTypes)
                {
                    var module = (IModule)Activator.CreateInstance(type)!;
                    module.Initialize(serviceProvider);
                    Console.WriteLine($"Executing module: {module.Name}");
                    module.Execute();
                }

                context.Unload();
            }
        }
    }
}
