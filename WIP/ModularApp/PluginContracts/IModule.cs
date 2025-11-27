using System;
using System.Collections.Generic;
using System.Text;

namespace PluginContracts
{
    public interface IModule
    {
        string Name { get; }
        void Initialize(IServiceProvider serviceProvider); // Inject config, SB, DB, Kafka, Redis
        void Execute(); // Host-invoked logic
    }
}
