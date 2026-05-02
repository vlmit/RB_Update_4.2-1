using System;
using System.Collections.Generic;
using Tessa.Discovery;

namespace Tessa.Extensions.Default.Console.PrintDiscoveryInfo
{
    public sealed class DiscoveryComponentsInfo
    {
        public List<DiscoveryComponent> Components { get; set; } = new();

        public Dictionary<string, PluginState?> Plugins { get; set; } = new();
        
        public DateTime RedisTime { get; set; }
    }
}
