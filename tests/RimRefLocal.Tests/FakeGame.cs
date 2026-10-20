using System.Reflection;
using System.Reflection.Emit;

namespace RimRefLocal.Tests;

public enum GameLayout
{
    // 1.7 and later keep the assemblies in the game folder.
    Root,
    Windows,
    Linux,
    // The game's files, its Mods folder included, are inside RimWorldMac.app.
    MacOS,
}

// A RimWorld folder with a RimRef package's assemblies where the game keeps its own. The targets only look at file names,
// Version.txt and Assembly-CSharp's version, so RimRef's files can stand in for the game's. Being RimRef's own files, the fake
// game is the same build as that RimRef until ReplaceAssemblyCSharp makes it another.
internal sealed class FakeGame
{
    private FakeGame(string folder, GameLayout layout)
    {
        Folder = folder;
        FilesFolder = layout == GameLayout.MacOS ? Path.Combine(folder, "RimWorldMac.app") : folder;
        ManagedFolder = layout switch
        {
            GameLayout.Root => folder,
            GameLayout.Windows => Path.Combine(folder, "RimWorldWin64_Data", "Managed"),
            GameLayout.Linux => Path.Combine(folder, "RimWorldLinux_Data", "Managed"),
            GameLayout.MacOS => Path.Combine(FilesFolder, "Contents", "Resources", "Data", "Managed"),
            _ => throw new ArgumentOutOfRangeException(nameof(layout)),
        };
    }

    internal string Folder { get; }

    internal string ManagedFolder { get; }

    internal string ModsFolder => Path.Combine(FilesFolder, "Mods");

    private string FilesFolder { get; }

    // Version.txt holds the game version RimRef's version starts with, unless a test sets another.
    internal static FakeGame Create(string parentFolder, string rimRefVersion, GameLayout layout)
    {
        string folder = Path.Combine(parentFolder, "RimWorld");
        var game = new FakeGame(folder, layout);
        Directory.CreateDirectory(game.ManagedFolder);

        foreach (string file in Directory.GetFiles(RimRef.AssembliesFolder(rimRefVersion)))
        {
            string destination = Path.Combine(game.ManagedFolder, Path.GetFileName(file));
            File.Copy(file, destination);
        }

        string gameVersion = rimRefVersion.Split('-')[0];
        game.SetVersion(gameVersion);
        File.WriteAllText(Path.Combine(game.FilesFolder, "ModUpdating.txt"), "");

        return game;
    }

    internal void SetVersion(string gameVersion)
    {
        File.WriteAllText(Path.Combine(FilesFolder, "Version.txt"), $"{gameVersion} rev1");
    }

    // Another build of the game, such as RimWorld 1.6 for Linux or macOS, has an Assembly-CSharp with another version.
    internal void ReplaceAssemblyCSharp()
    {
        var name = new AssemblyName("Assembly-CSharp") { Version = new Version(9, 9, 9, 9) };
        var assembly = new PersistedAssemblyBuilder(name, typeof(object).Assembly);
        assembly.DefineDynamicModule("Assembly-CSharp");

        string path = Path.Combine(ManagedFolder, "Assembly-CSharp.dll");
        File.Delete(path);
        assembly.Save(path);
    }
}
