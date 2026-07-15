using NetArchTest.Rules;
using Xunit;

namespace YG.ArchitectureTests;

/// <summary>
/// Rule 3: modules never touch HttpContext or claims. Identity flows in through the
/// IUserContext abstraction; authorization is declared, not inspected. If a handler
/// needs to know who is calling, it takes it as data on the message.
/// </summary>
public class HttpIsolationTests
{
    [Fact]
    public void Modules_never_touch_HttpContext_or_claims()
    {
        foreach (var (name, assembly) in TestAssemblies.Modules)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot().HaveDependencyOnAny(
                    "Microsoft.AspNetCore.Http",
                    "System.Security.Claims")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"Module '{name}' touches HttpContext/claims directly in: " +
                string.Join(", ", result.FailingTypeNames ?? []));
        }
    }
}
