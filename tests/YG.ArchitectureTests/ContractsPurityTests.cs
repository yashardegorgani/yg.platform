using Xunit;

namespace YG.ArchitectureTests;

/// <summary>
/// Rule 2: Contracts are the public doorway between modules, so they must carry
/// no baggage: no EF, no Wolverine, no FastEndpoints, no other YG project - only the runtime.
/// A contract that drags a dependency forces it on every consumer.
/// </summary>
public class ContractsPurityTests
{
    [Fact]
    public void Contracts_reference_nothing_but_the_runtime()
    {
        foreach (var (name, assembly) in TestAssemblies.Contracts)
        {
            var illegal = assembly.GetReferencedAssemblies()
                .Select(a => a.Name!)
                .Where(r => !r.StartsWith("System", StringComparison.Ordinal)
                            && r != "netstandard"
                            && r != "mscorlib")
                .ToArray();

            Assert.True(illegal.Length == 0,
                $"'{name}' must be dependency-free but references: {string.Join(", ", illegal)}");
        }
    }
}
