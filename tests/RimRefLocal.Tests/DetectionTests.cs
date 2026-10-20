using NUnit.Framework;

namespace RimRefLocal.Tests;

public class DetectionTests
{
    [TestCase(GameLayout.Windows)]
    [TestCase(GameLayout.MacOS)]
    public void ProjectInsideRimWorld_FindsIt(GameLayout layout)
    {
        using var folder = new TemporaryFolder();
        FakeGame game = FakeGame.Create(folder.Path, RimRef.Version16, layout);
        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net472</TargetFramework>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Krafs.Rimworld.Ref" Version="{RimRef.Version16}" />
              </ItemGroup>

            </Project>
            """;
        string modFolder = Path.Combine(game.ModsFolder, "My Mod", "Source");
        string project = Mod.Create(modFolder, csproj);

        BuildResult result = Runner.Build(project);

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Reference("Assembly-CSharp.dll"), Does.StartWith(game.ManagedFolder), result.Output);
    }

    // Outside Windows, Steam is in the home folder, so the test gives the build a fake one. On Windows it's in the
    // registry, which a test can't fake.
    [TestCase(".local/share/Steam", IncludePlatform = "Linux")]
    [TestCase("Library/Application Support/Steam", IncludePlatform = "MacOsX")]
    public void SteamLibrary_FindsIt(string steamFolder)
    {
        using var folder = new TemporaryFolder();
        string otherLibrary = Path.Combine(folder.Path, "Other Library");
        string rimWorldLibrary = Path.Combine(folder.Path, "RimWorld Library");
        string commonFolder = Path.Combine(rimWorldLibrary, "steamapps", "common");
        FakeGame game = FakeGame.Create(commonFolder, RimRef.Version16, GameLayout.Windows);

        // Steam's list of libraries and the apps in each. RimWorld's app id is 294100. The other library has an app whose id
        // contains RimWorld's, and RimWorld's id as a value, so only RimWorld's library may match.
        string libraryFolders = $$"""
            "libraryfolders"
            {
            	"0"
            	{
            		"path"		"{{otherLibrary}}"
            		"apps"
            		{
            			"2294100"		"1"
            			"228980"		"294100"
            		}
            	}
            	"1"
            	{
            		"path"		"{{rimWorldLibrary}}"
            		"apps"
            		{
            			"294100"		"1"
            		}
            	}
            }
            """;
        string home = Path.Combine(folder.Path, "home");
        string steamApps = Path.Combine(home, steamFolder, "steamapps");
        Directory.CreateDirectory(steamApps);
        File.WriteAllText(Path.Combine(steamApps, "libraryfolders.vdf"), libraryFolders);

        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net472</TargetFramework>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Krafs.Rimworld.Ref" Version="{RimRef.Version16}" />
              </ItemGroup>

            </Project>
            """;
        string project = Mod.Create(Path.Combine(folder.Path, "Mod"), csproj);

        // NuGet's package folder is in the home folder too, so point it at the real one.
        var environment = new Dictionary<string, string>
        {
            ["HOME"] = home,
            ["NUGET_PACKAGES"] = Repository.PackageRoot,
        };
        BuildResult result = Runner.Build(project, environment);

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Reference("Assembly-CSharp.dll"), Does.StartWith(game.ManagedFolder), result.Output);
    }

    [Test]
    public void MissingRimWorld_KeepsRimRefWithoutWarnings()
    {
        using var folder = new TemporaryFolder();
        string missingFolder = Path.Combine(folder.Path, "RimWorld");
        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net472</TargetFramework>
                <RimWorldDirectory>{missingFolder}</RimWorldDirectory>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Krafs.Rimworld.Ref" Version="{RimRef.Version16}" />
              </ItemGroup>

            </Project>
            """;
        string project = Mod.Create(Path.Combine(folder.Path, "Mod"), csproj);

        BuildResult result = Runner.Build(project);

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Reference("Assembly-CSharp.dll"), Does.StartWith(RimRef.Folder(RimRef.Version16)), result.Output);
        Assert.That(result.Output, Does.Not.Contain("warning"), result.Output);
    }
}
