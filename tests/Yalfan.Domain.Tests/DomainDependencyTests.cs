using System.Reflection;

namespace Yalfan.Domain.Tests;

public class DomainDependencyTests
{
    /// <summary>
    /// Architecture rule: the Domain references no other project and no EF Core. This fails as
    /// soon as Domain code uses a type from any assembly outside the .NET base class library.
    /// </summary>
    [Fact]
    public void Domain_uses_nothing_outside_the_base_class_library()
    {
        var domain = Assembly.Load("Yalfan.Domain");

        var foreignReferences = domain.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => !IsBaseClassLibrary(name))
            .ToList();

        Assert.Empty(foreignReferences);
    }

    private static bool IsBaseClassLibrary(string assemblyName) =>
        assemblyName is "netstandard" or "mscorlib" or "System"
        || assemblyName.StartsWith("System.", StringComparison.Ordinal);
}
