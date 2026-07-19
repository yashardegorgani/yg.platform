using NetArchTest.Rules;
using Xunit;

namespace YG.ArchitectureTests;


public class MessagingTests
{
    [Fact]
    public void Modules_do_not_touch_wolverine_directly()
    {
        foreach (var (name, assembly) in TestAssemblies.Modules)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot().HaveDependencyOn("Wolverine")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"{name} uses Wolverine directly: " +
                string.Join(", ", result.FailingTypeNames ?? []));
        }
    }
}
