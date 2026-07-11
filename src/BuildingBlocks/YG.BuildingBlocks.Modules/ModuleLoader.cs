using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace YG.BuildingBlocks.Modules;

public static class ModuleLoader
{
    public static IReadOnlyList<IYGModule> LoadModules(
        IConfiguration configuration,
        params string[] probingPaths)
    {
        var modules = new List<IYGModule>();

        foreach (var path in probingPaths.Select(Path.GetFullPath).Where(Directory.Exists))
        {
            foreach (var dll in Directory.EnumerateFiles(
                path, "YG.Modules.*.dll", SearchOption.AllDirectories))
            {
                // Contracts assemblies are message definitions, not modules (step 6).
                if (Path.GetFileName(dll).EndsWith(".Contracts.dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                // LoadFrom = the DEFAULT AssemblyLoadContext, on purpose:
                // the plugin's IYGModule must be the SAME .NET type as the Host's.
                var assembly = Assembly.LoadFrom(dll);

                foreach (var type in assembly.GetTypes().Where(t =>
                    typeof(IYGModule).IsAssignableFrom(t)
                    && t is { IsAbstract: false, IsInterface: false }))
                {
                    var module = (IYGModule)Activator.CreateInstance(type)!;

                    // Config decides whether a discovered module actually runs.
                    if (configuration.GetValue($"Modules:{module.Name}:Enabled", defaultValue: true))
                        modules.Add(module);
                }
            }
        }

        return modules.DistinctBy(m => m.Name).ToList();
    }
}
