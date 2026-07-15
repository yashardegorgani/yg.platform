using NetArchTest.Rules;
using Xunit;

namespace YG.ArchitectureTests;

/// <summary>
/// Rule 5: naming and placement conventions. Vertical slices are a discipline,
/// not a suggestion - endpoints and handlers live in Features, DbContexts in Persistence.
/// </summary>
public class ConventionTests
{
    [Fact]
    public void Endpoints_live_in_features()
    {
        foreach (var (name, assembly) in TestAssemblies.Modules)
        {
            var result = Types.InAssembly(assembly)
                .That().HaveNameEndingWith("Endpoint")
                .Should().ResideInNamespaceContaining(".Features")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"Endpoints outside Features in '{name}': " +
                string.Join(", ", result.FailingTypeNames ?? []));
        }
    }

    [Fact]
    public void Handlers_live_in_features()
    {
        foreach (var (name, assembly) in TestAssemblies.Modules)
        {
            var result = Types.InAssembly(assembly)
                .That().HaveNameEndingWith("Handler")
                .Should().ResideInNamespaceContaining(".Features")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"Handlers outside Features in '{name}': " +
                string.Join(", ", result.FailingTypeNames ?? []));
        }
    }

    [Fact]
    public void DbContexts_live_in_persistence()
    {
        foreach (var (name, assembly) in TestAssemblies.Modules)
        {
            var result = Types.InAssembly(assembly)
                .That().HaveNameEndingWith("DbContext")
                .Should().ResideInNamespaceContaining(".Persistence")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"DbContexts outside Persistence in '{name}': " +
                string.Join(", ", result.FailingTypeNames ?? []));
        }
    }
}
