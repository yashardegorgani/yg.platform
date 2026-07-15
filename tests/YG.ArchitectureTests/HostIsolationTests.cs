using NetArchTest.Rules;
using Xunit;

namespace YG.ArchitectureTests;

/// <summary>
/// Rule 4: nobody references the Host. The Host loads modules; the arrow never
/// points back. (The compiler mostly enforces this via missing project references -
/// this test catches the workarounds, like a copied DLL or a future shared project.)
/// </summary>
public class HostIsolationTests
{
    [Fact]
    public void Nothing_references_the_host()
    {
        var everything = TestAssemblies.Modules
            .Concat(TestAssemblies.Contracts)
            .Concat(TestAssemblies.BuildingBlocks);

        foreach (var (name, assembly) in everything)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot().HaveDependencyOn("YG.Host")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"'{name}' references YG.Host in: " +
                string.Join(", ", result.FailingTypeNames ?? []));
        }
    }
}
