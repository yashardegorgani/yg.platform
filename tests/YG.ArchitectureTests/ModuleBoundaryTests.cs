using NetArchTest.Rules;
using Xunit;

namespace YG.ArchitectureTests;

/// <summary>
/// Rule 1: a module may only touch another module through its Contracts project.
/// Internals (Domain, Features, Persistence, Infrastructure, Migrations) are off-limits.
/// </summary>
public class ModuleBoundaryTests
{
    private static string[] InternalNamespacesOf(string moduleName) =>
    [
        $"YG.Modules.{moduleName}.Domain",
        $"YG.Modules.{moduleName}.Features",
        $"YG.Modules.{moduleName}.Persistence",
        $"YG.Modules.{moduleName}.Infrastructure",
        $"YG.Modules.{moduleName}.Migrations",
    ];

    [Fact]
    public void Modules_may_only_touch_other_modules_through_contracts()
    {
        foreach (var (name, assembly) in TestAssemblies.Modules)
        {
            var forbidden = TestAssemblies.ModuleNames
                .Where(other => other != name)
                .SelectMany(InternalNamespacesOf)
                .ToArray();

            var result = Types.InAssembly(assembly)
                .ShouldNot().HaveDependencyOnAny(forbidden)
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"Module '{name}' reaches into another module's internals via: " +
                string.Join(", ", result.FailingTypeNames ?? []));
        }
    }

    [Fact]
    public void Building_blocks_know_no_modules()
    {
        var moduleRoots = TestAssemblies.ModuleNames
            .Select(m => $"YG.Modules.{m}")
            .ToArray();

        foreach (var (name, assembly) in TestAssemblies.BuildingBlocks)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot().HaveDependencyOnAny(moduleRoots)
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"'{name}' depends on a module (blocks must stay module-agnostic): " +
                string.Join(", ", result.FailingTypeNames ?? []));
        }
    }
}
