using System.Reflection;
using System.Text.RegularExpressions;
using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.Tests.Architecture;

public sealed class EngineArchitectureGuardTests
{
    private static readonly Regex[] ForbiddenSourcePatterns =
    [
        new(@"using\s+Microsoft\.Xna\.Framework", RegexOptions.CultureInvariant),
        new(@"\bGameTime\b", RegexOptions.CultureInvariant),
        new(@"\bGraphicsDevice\b", RegexOptions.CultureInvariant),
        new(@"\bSpriteBatch\b", RegexOptions.CultureInvariant),
        new(@"\bTexture2D\b", RegexOptions.CultureInvariant),
        new(@"\bColor\b", RegexOptions.CultureInvariant),
        new(@"\bRectangle\b", RegexOptions.CultureInvariant),
        new(@"\bVector2\b", RegexOptions.CultureInvariant),
        new(@"\bMatrix\b", RegexOptions.CultureInvariant)
    ];
    private static readonly Regex StringLiteralPattern = new("\"(?:[^\"\\\\]|\\\\.)*\"", RegexOptions.CultureInvariant);
    private static readonly Regex SingleLineCommentPattern = new(@"//.*?$", RegexOptions.CultureInvariant | RegexOptions.Multiline);
    private static readonly Regex MultiLineCommentPattern = new(@"/\*.*?\*/", RegexOptions.CultureInvariant | RegexOptions.Singleline);

    [Fact]
    public void EngineSource_DoesNotReferenceForbiddenMonoGameTokens()
    {
        var engineSourceFiles = Directory
            .EnumerateFiles(GetEngineSourceRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        foreach (var file in engineSourceFiles)
        {
            var source = SanitizeSource(File.ReadAllText(file));

            foreach (var pattern in ForbiddenSourcePatterns)
            {
                Assert.False(
                    pattern.IsMatch(source),
                    $"Source file '{file}' matched forbidden MonoGame pattern '{pattern}'.");
            }
        }
    }

    [Fact]
    public void PublicApi_DoesNotExposeMonoGameTypes()
    {
        var exportedTypes = typeof(Int2).Assembly.GetExportedTypes();

        foreach (var exportedType in exportedTypes)
        {
            Assert.False(IsForbiddenType(exportedType), $"Public type '{exportedType.FullName}' exposes a MonoGame type.");

            foreach (var implementedInterface in exportedType.GetInterfaces())
            {
                Assert.False(
                    IsForbiddenType(implementedInterface),
                    $"Public type '{exportedType.FullName}' implements forbidden interface '{implementedInterface.FullName}'.");
            }

            if (exportedType.BaseType is not null)
            {
                Assert.False(
                    IsForbiddenType(exportedType.BaseType),
                    $"Public type '{exportedType.FullName}' derives from forbidden base type '{exportedType.BaseType.FullName}'.");
            }

            foreach (var property in exportedType.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
            {
                Assert.False(
                    IsForbiddenType(property.PropertyType),
                    $"Property '{exportedType.FullName}.{property.Name}' exposes forbidden type '{property.PropertyType.FullName}'.");
            }

            foreach (var field in exportedType.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
            {
                Assert.False(
                    IsForbiddenType(field.FieldType),
                    $"Field '{exportedType.FullName}.{field.Name}' exposes forbidden type '{field.FieldType.FullName}'.");
            }

            foreach (var method in exportedType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
            {
                Assert.False(
                    IsForbiddenType(method.ReturnType),
                    $"Method '{exportedType.FullName}.{method.Name}' returns forbidden type '{method.ReturnType.FullName}'.");

                foreach (var parameter in method.GetParameters())
                {
                    Assert.False(
                        IsForbiddenType(parameter.ParameterType),
                        $"Method '{exportedType.FullName}.{method.Name}' accepts forbidden type '{parameter.ParameterType.FullName}'.");
                }
            }

            foreach (var constructor in exportedType.GetConstructors(BindingFlags.Instance | BindingFlags.Public))
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    Assert.False(
                        IsForbiddenType(parameter.ParameterType),
                        $"Constructor '{exportedType.FullName}' accepts forbidden type '{parameter.ParameterType.FullName}'.");
                }
            }
        }
    }

    [Fact]
    public void EngineAndDesktopProjects_DoNotReferenceMonoGamePackagesDirectly()
    {
        var repoRoot = GetRepositoryRoot();
        var engineProject = File.ReadAllText(Path.Combine(repoRoot, "TileWorld.Engine", "TileWorld.Engine.csproj"));
        var desktopProject = File.ReadAllText(Path.Combine(repoRoot, "TileWorld.Testing.Desktop", "TileWorld.Testing.Desktop.csproj"));
        var monoGameHostingProject = File.ReadAllText(Path.Combine(repoRoot, "TileWorld.Engine.Hosting.MonoGame", "TileWorld.Engine.Hosting.MonoGame.csproj"));

        Assert.DoesNotContain("<PackageReference Include=\"MonoGame.", engineProject, StringComparison.Ordinal);
        Assert.DoesNotContain("<PackageReference Include=\"MonoGame.", desktopProject, StringComparison.Ordinal);
        Assert.Contains("MonoGame.Framework.DesktopGL", monoGameHostingProject, StringComparison.Ordinal);
    }

    private static string GetEngineSourceRoot()
    {
        return Path.Combine(GetRepositoryRoot(), "TileWorld.Engine");
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "TileWorldEngine.sln");
            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the TileWorldEngine repository root.");
    }

    private static bool IsForbiddenType(Type type)
    {
        var pendingTypes = new Queue<Type>();
        var visitedTypes = new HashSet<Type>();

        pendingTypes.Enqueue(type);

        while (pendingTypes.Count > 0)
        {
            var currentType = pendingTypes.Dequeue();

            if (!visitedTypes.Add(currentType) || currentType == typeof(void))
            {
                continue;
            }

            var namespaceName = currentType.Namespace ?? string.Empty;
            if (namespaceName.StartsWith("Microsoft.Xna.Framework", StringComparison.Ordinal))
            {
                return true;
            }

            if (currentType.HasElementType && currentType.GetElementType() is { } elementType)
            {
                pendingTypes.Enqueue(elementType);
            }

            if (currentType.IsGenericType)
            {
                if (!currentType.IsGenericTypeDefinition)
                {
                    pendingTypes.Enqueue(currentType.GetGenericTypeDefinition());
                }

                foreach (var genericArgument in currentType.GetGenericArguments())
                {
                    pendingTypes.Enqueue(genericArgument);
                }
            }
        }

        return false;
    }

    private static string SanitizeSource(string source)
    {
        var withoutStrings = StringLiteralPattern.Replace(source, "\"\"");
        var withoutBlockComments = MultiLineCommentPattern.Replace(withoutStrings, string.Empty);
        return SingleLineCommentPattern.Replace(withoutBlockComments, string.Empty);
    }
}
