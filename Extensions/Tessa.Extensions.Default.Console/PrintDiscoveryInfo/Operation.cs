using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Discovery;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Json;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Console.PrintDiscoveryInfo
{
    public sealed class Operation
    {
        #region Private Fields

        private readonly IComponentsProvider componentsProvider;

        #endregion

        #region Constructors

        public Operation(
            IComponentsProvider componentsProvider,
            IConsoleLogger consoleLogger)
        {
            this.componentsProvider = NotNullOrThrow(componentsProvider);
        }

        #endregion

        #region Public Methods

        public async Task<int> ExecuteAsync(
            OperationContext context,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);

            var stdOut = context.StdOut;

            var componentsInfo = await this.componentsProvider.GetComponentsAsync(context is { PrintInfo: true, Export: false }, cancellationToken);

            var currentTime = new DateTimeOffset(componentsInfo.RedisTime);
            // Не показываем компоненты, которые появлялись больше одного дня назад
            var components = componentsInfo.Components.Where(x => currentTime - x.LastSeen < TimeSpan.FromDays(1)).ToList();
            if (context.Export)
            {
                var comps = components
                    .Select(x => x.Info)
                    .ToList();
                await stdOut.WriteAsync(StorageHelper.SerializeToJson(comps, TessaSerializer.Json, indented: true));
                return 0;
            }

            var componentsByType = new Dictionary<string, List<DiscoveryComponent>>(StringComparer.Ordinal);
            foreach (var component in components)
            {
                ThrowIfNull(component.Info);

                // TODO: still handle unknown components
                if (component.Info.ComponentType is null)
                {
                    continue;
                }

                if (!componentsByType.TryGetValue(component.Info.ComponentType, out var instances))
                {
                    instances = new List<DiscoveryComponent>();
                    componentsByType[component.Info.ComponentType] = instances;
                }

                instances.Add(component);
            }

            foreach (var (componentType, instances) in componentsByType)
            {
                if (componentType == DiscoveryHelper.WebComponentTypeName)
                {
                    // TODO: Выводить общее состояние на основе состояний всех дочерних
                    //OutputState(stdOut, "OK");
                    // TODO: print machine name
                    await stdOut.WriteLineAsync($" {componentType} ({instances.Count} procs):");
                    foreach (var instance in instances)
                    {
                        ThrowIfNull(instance.Info);
                        var pid = instance.Info.Info.TryGet<long?>("PID") ?? -1;
                        var lastSeenText = GetLastSeenText(instance.LastSeen, currentTime);
                        await stdOut.WriteAsync("    ");
                        OutputState(stdOut, instance.Info.State);
                        await stdOut.WriteLineAsync($" {instance.Name} {pid} last seen {lastSeenText}");

                        await TryPrintAdditionalInfoAsync(context, stdOut, instance, indent: 8);
                        await TryPrintPluginsInfoAsync(context, stdOut, instance, componentsInfo, 12);
                    }

                    continue;
                }

                foreach (var instance in instances)
                {
                    ThrowIfNull(instance.Info);
                    var lastSeenText = GetLastSeenText(instance.LastSeen, currentTime);
                    OutputState(stdOut, instance.Info.State);
                    await stdOut.WriteLineAsync($" {componentType} ({instance.Name}) last seen {lastSeenText}");

                    await TryPrintAdditionalInfoAsync(context, stdOut, instance, indent: 4);
                    await TryPrintPluginsInfoAsync(context, stdOut, instance, componentsInfo, 8);
                }
            }

            return 0;
        }

        #endregion

        #region Private Methods

        private static async Task TryPrintAdditionalInfoAsync(
            OperationContext context,
            TextWriter stdOut,
            DiscoveryComponent component,
            int indent)
        {
            if (!context.PrintInfo)
            {
                return;
            }

            var additionalInfo = component.Info?.Info;
            if (additionalInfo is null)
            {
                return;
            }

            try
            {
                var hasProcessCpuUsage = TryGetProcessCpuUsage(additionalInfo, out var processCpuUsage);
                var hasProcessMemoryUsage = TryGetProcessMemoryUsage(additionalInfo, out var processMemoryUsage);
                var hasTotalCpuUsage = TryGetTotalCpuUsage(additionalInfo, out var totalCpuUsage);
                var hasTotalMemoryUsage = TryGetTotalMemoryUsage(additionalInfo, out var totalMemoryUsage);
                var hasDiskQueueLength = TryGetDiskQueueLength(additionalInfo, out var diskQueueLength);
                var hasIsCurrentScheduler = TryGetIsCurrentScheduler(additionalInfo, out var isCurrentScheduler);

                if (!(hasProcessCpuUsage || hasProcessMemoryUsage || hasTotalCpuUsage || hasTotalMemoryUsage ||
                    hasDiskQueueLength || hasIsCurrentScheduler))
                {
                    return;
                }

                var spaces = new string(' ', indent);
                if (hasProcessCpuUsage || hasProcessMemoryUsage)
                {
                    await stdOut.WriteAsync(spaces);

                    var addSeparator = false;

                    if (hasProcessCpuUsage)
                    {
                        await stdOut.WriteAsync($"CPU usage: {processCpuUsage}%");
                        addSeparator = true;
                    }

                    if (hasProcessMemoryUsage)
                    {
                        if (addSeparator)
                        {
                            await stdOut.WriteAsync(", ");
                        }

                        await stdOut.WriteAsync($"Memory usage: {processMemoryUsage}%");
                        //addSeparator = true;
                    }

                    await stdOut.WriteLineAsync();
                }

                if (hasTotalCpuUsage || hasTotalMemoryUsage || hasDiskQueueLength)
                {
                    await stdOut.WriteAsync(spaces);

                    var addSeparator = false;

                    if (hasTotalCpuUsage)
                    {
                        await stdOut.WriteAsync($"Total CPU usage: {totalCpuUsage}%");
                        addSeparator = true;
                    }

                    if (hasTotalMemoryUsage)
                    {
                        if (addSeparator)
                        {
                            await stdOut.WriteAsync(", ");
                        }

                        await stdOut.WriteAsync($"Total Memory usage: {totalMemoryUsage}%");
                        addSeparator = true;
                    }

                    if (hasDiskQueueLength)
                    {
                        if (addSeparator)
                        {
                            await stdOut.WriteAsync(", ");
                        }

                        await stdOut.WriteAsync($"Disk Queue Length: {diskQueueLength:0.000}");
                        //addSeparator = true;
                    }

                    await stdOut.WriteLineAsync();
                }

                if (hasIsCurrentScheduler)
                {
                    await stdOut.WriteAsync(spaces);
                    await stdOut.WriteAsync(
                        isCurrentScheduler
                        ? $"Processes all plugins"
                        : $"Processes only operations plugins");
                    await stdOut.WriteLineAsync();
                }
            }
            catch
            {
                // Не можем вывести информацию о счётчиках, просто выходим
            }

            static bool TryGetProcessCpuUsage(Dictionary<string, object?> additionalInfo, out double processCpuUsage)
            {
                processCpuUsage = default;
                return additionalInfo.TryGet<string>("ProcessCPUUsage") is { } cpuUsageString
                    && double.TryParse(cpuUsageString, NumberStyles.Any, CultureInfo.InvariantCulture,
                        out processCpuUsage);
            }

            static bool TryGetProcessMemoryUsage(Dictionary<string, object?> additionalInfo, out long processMemoryUsage)
            {
                processMemoryUsage = default;
                var processMemoryUsageLocal = additionalInfo.TryGet<long?>("ProcessMemoryUsage");
                if (processMemoryUsageLocal.HasValue)
                {
                    processMemoryUsage = processMemoryUsageLocal.Value;
                    return true;
                }

                return false;
            }

            static bool TryGetTotalCpuUsage(Dictionary<string, object?> additionalInfo, out double totalCpuUsage)
            {
                totalCpuUsage = default;
                return additionalInfo.TryGet<string>("TotalCPUUsage") is { } totalCpuUsageString
                    && double.TryParse(totalCpuUsageString, NumberStyles.Any, CultureInfo.InvariantCulture,
                        out totalCpuUsage);
            }

            static bool TryGetTotalMemoryUsage(Dictionary<string, object?> additionalInfo, out long totalMemoryUsage)
            {
                totalMemoryUsage = default;
                var totalMemoryUsageLocal = additionalInfo.TryGet<long?>("TotalMemoryUsage");
                if (totalMemoryUsageLocal.HasValue)
                {
                    totalMemoryUsage = totalMemoryUsageLocal.Value;
                    return true;
                }

                return false;
            }

            static bool TryGetDiskQueueLength(Dictionary<string, object?> additionalInfo, out double diskQueueLength)
            {
                diskQueueLength = default;
                return additionalInfo.TryGet<string>("DiskQueueLength") is { } diskQueueLengthString
                    && double.TryParse(diskQueueLengthString, NumberStyles.Any, CultureInfo.InvariantCulture,
                        out diskQueueLength);
            }

            static bool TryGetIsCurrentScheduler(Dictionary<string, object?> additionalInfo, out bool isCurrentScheduler)
            {
                isCurrentScheduler = default;
                if (additionalInfo.TryGet<bool?>("IsCurrentScheduler") is { } isCurrentSchedulerLocal)
                {
                    isCurrentScheduler = isCurrentSchedulerLocal;
                    return true;
                }

                return false;
            }
        }

        private static async Task TryPrintPluginsInfoAsync(
            OperationContext context,
            TextWriter stdOut,
            DiscoveryComponent component,
            DiscoveryComponentsInfo componentsInfo,
            int indent)
        {
            if (!context.PrintInfo)
            {
                return;
            }

            var pluginsList = component.Info?.Info.TryGet<List<object?>>("Plugins");
            if (pluginsList is not { Count: > 0 })
            {
                return;
            }

            var spaces = new string(' ', indent);
            var spacesNext = new string(' ', indent + 4);
            var plugins = new ListStorage<DiscoveryPlugin>(pluginsList, DiscoveryPlugin.PluginFactory);
            foreach (var plugin in plugins)
            {
                ThrowIfNull(plugin.CidName);
                await stdOut.WriteLineAsync($"{spaces}{plugin.Name} {plugin.PID} {plugin.LastRun.ToLocalTime()} {plugin.Duration}");
                if (componentsInfo.Plugins.TryGetValue(plugin.CidName, out var pluginState))
                {
                    await stdOut.WriteAsync(spacesNext);
                    OutputState(stdOut, pluginState?.State);
                    await stdOut.WriteLineAsync($" ({pluginState?.StateDescription})");
                }
            }
        }

        private static string GetLastSeenText(
            DateTimeOffset? timestamp,
            DateTimeOffset currentTime)
        {
            var lastSeenText = "unknown";
            if (timestamp.HasValue)
            {
                var lastSeenDelta = currentTime - timestamp.Value;

                if (lastSeenDelta < TimeSpan.FromSeconds(30))
                {
                    lastSeenText = "<30sec";
                }
                else if (lastSeenDelta < TimeSpan.FromMinutes(1))
                {
                    lastSeenText = "<1min";
                }
                else if (lastSeenDelta < TimeSpan.FromHours(1))
                {
                    lastSeenText = $"{lastSeenDelta.Minutes}min";
                }
                else
                {
                    lastSeenText = ">1hour";
                }
            }

            return lastSeenText;
        }

        private static void OutputState(
            TextWriter stdOut,
            string? state)
        {
            var previousColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = GetColorForState(state);
            stdOut.Write(state);
            System.Console.ForegroundColor = previousColor;
        }

        private static ConsoleColor GetColorForState(string? state)
        {
            return state?.ToUpperInvariant() switch
            {
                DiscoveryHelper.CommandResponseStateOK => ConsoleColor.Green,
                DiscoveryHelper.CommandResponseStateWarning => ConsoleColor.Yellow,
                DiscoveryHelper.CommandResponseStateError => ConsoleColor.Red,
                _ => ConsoleColor.Gray
            };
        }

        #endregion
    }
}
