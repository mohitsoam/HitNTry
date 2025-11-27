using System;
using System.Collections.Generic;
using System.Text;

namespace PluginContracts
{
    public interface IModule
    {
        string Name { get; }
        void Execute();
    }
}
