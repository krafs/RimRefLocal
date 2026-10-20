namespace RimRefLocal.Tests;

// The RimRef packages the tests use. The csproj downloads them, so they're always in the package folder.
internal static class RimRef
{
    // For RimWorld 1.6, targeting net472. Stands in for every version before 1.7.
    internal const string Version16 = "1.6.4871";

    // For RimWorld 1.7, targeting net10.0.
    internal const string Version17 = "1.7.4977-alpha.2";

    internal static string Folder(string version)
    {
        return Path.Combine(Repository.PackageRoot, "krafs.rimworld.ref", version);
    }

    // Each RimRef package has assemblies for one target framework.
    internal static string AssembliesFolder(string version)
    {
        string refFolder = Path.Combine(Folder(version), "ref");
        return Directory.GetDirectories(refFolder).Single();
    }
}
