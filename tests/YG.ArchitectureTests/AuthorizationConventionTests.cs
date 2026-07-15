using Xunit;

namespace YG.ArchitectureTests;

/// <summary>
/// Rule 6 (earned by Option A): feature code declares Permissions("..."), never Roles("...").
/// Role names are runtime data owned by the Access module; the moment an endpoint
/// names a role, authorization is compiled in again.
///
/// NetArchTest inspects type dependencies, not method calls, so this rule is enforced
/// by scanning the source tree - cruder, but exactly as effective.
/// </summary>
public class AuthorizationConventionTests
{
    [Fact]
    public void Feature_code_never_names_roles()
    {
        var modulesDir = Path.Combine(FindSrcDirectory(), "Modules");

        var offenders = Directory
            .EnumerateFiles(modulesDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Contains("obj")
                     && !f.Split(Path.DirectorySeparatorChar).Contains("bin"))
            .Where(f => File.ReadAllText(f).Contains("Roles(\"", StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(modulesDir, f))
            .ToArray();

        Assert.True(offenders.Length == 0,
            "Roles(\"...\") found in module code - use Permissions(\"...\") instead:\n" +
            string.Join("\n", offenders));
    }

    [Fact]
    public void Declared_permissions_follow_the_naming_convention()
    {
        var modulesDir = Path.Combine(FindSrcDirectory(), "Modules");
        var pattern = new System.Text.RegularExpressions.Regex(
            "Permissions\\(\"(?<p>[^\"]+)\"\\)");
        var shape = new System.Text.RegularExpressions.Regex(
            "^[a-z0-9-]+(\\.[a-z0-9-]+)+$");

        var offenders = new List<string>();

        foreach (var file in Directory
            .EnumerateFiles(modulesDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Contains("obj")
                     && !f.Split(Path.DirectorySeparatorChar).Contains("bin")))
        {
            foreach (System.Text.RegularExpressions.Match m in pattern.Matches(File.ReadAllText(file)))
            {
                var permission = m.Groups["p"].Value;
                if (!shape.IsMatch(permission))
                    offenders.Add($"{Path.GetRelativePath(modulesDir, file)}: \"{permission}\"");
            }
        }

        Assert.True(offenders.Count == 0,
            "Permissions must look like 'module.resource.action':\n" + string.Join("\n", offenders));
    }

    private static string FindSrcDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src");
            if (Directory.Exists(Path.Combine(candidate, "Modules")))
                return candidate;
            dir = dir.Parent!;
        }
        throw new InvalidOperationException(
            $"Could not locate the repo's 'src' directory above {AppContext.BaseDirectory}");
    }
}
