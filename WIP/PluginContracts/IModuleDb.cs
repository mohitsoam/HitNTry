using System;
using System.Collections.Generic;
using System.Text;

namespace PluginContracts
{
    public interface IModuleDb
    {
        string Name { get; }
        void Initialize(IServiceProvider serviceProvider); // Host injects dependencies
        void Execute(); // Module logic
    }
}
