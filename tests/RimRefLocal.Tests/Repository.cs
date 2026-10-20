using System.Reflection;

namespace RimRefLocal.Tests;

internal static class Repository
{
    internal static string Root => GetMetadata("RepositoryRoot");

    internal static string TargetsFile => Path.Combine(Root, "src", "build", "Krafs.Rimworld.Ref.Local.targets");

    internal static string Nuspec => Path.Combine(Root, "src", "Krafs.Rimworld.Ref.Local.nuspec");

    // Where NuGet put the packages this test project downloads.
    internal static string PackageRoot => GetMetadata("NuGetPackageRoot");

    private static string GetMetadata(string key)
    {
        var attributes = typeof(Repository).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
        var attribute = attributes.Single(attribute => attribute.Key == key);

        return attribute.Value!;
    }
}
