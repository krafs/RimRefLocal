namespace RimRefLocal.Tests;

internal static class Mod
{
    // Writes a mod project that gets the targets the copy-paste way, as Directory.Build.targets next to it.
    internal static string Create(string folder, string csproj)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "Mod.cs"), "namespace Mod { public static class Mod { } }");
        File.Copy(Repository.TargetsFile, Path.Combine(folder, "Directory.Build.targets"));

        string projectPath = Path.Combine(folder, "Mod.csproj");
        File.WriteAllText(projectPath, csproj);

        return projectPath;
    }
}
