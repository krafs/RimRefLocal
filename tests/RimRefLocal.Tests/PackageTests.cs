using NUnit.Framework;

namespace RimRefLocal.Tests;

// The other tests copy the targets file next to the mod. This one packs it and restores it like a modder would.
public class PackageTests
{
    [Test]
    public void Package_CompilesAgainstTheGame()
    {
        using var folder = new TemporaryFolder();
        string feed = Path.Combine(folder.Path, "feed");
        ProcessResult pack = Runner.Run("dotnet", "pack", Repository.Nuspec, "--output", feed);
        Assert.That(pack.ExitCode, Is.Zero, pack.Output);

        // Its own package folder, so a Krafs.Rimworld.Ref.Local from nuget.org, cached on this machine, can't stand in for it.
        string packages = Path.Combine(folder.Path, "packages");
        string nugetConfig = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <config>
                <clear />
                <add key="globalPackagesFolder" value="{packages}" />
              </config>
              <packageSources>
                <clear />
                <add key="feed" value="{feed}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
              <packageSourceMapping>
                <clear />
                <packageSource key="feed">
                  <package pattern="Krafs.Rimworld.Ref.Local" />
                </packageSource>
                <packageSource key="nuget.org">
                  <package pattern="*" />
                </packageSource>
              </packageSourceMapping>
            </configuration>
            """;
        File.WriteAllText(Path.Combine(folder.Path, "nuget.config"), nugetConfig);

        FakeGame game = FakeGame.Create(folder.Path, RimRef.Version17, GameLayout.Root);
        string modFolder = Path.Combine(folder.Path, "Mod");
        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <RimWorldDirectory>{game.Folder}</RimWorldDirectory>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Krafs.Rimworld.Ref" Version="{RimRef.Version17}" />
                <PackageReference Include="Krafs.Rimworld.Ref.Local" Version="*" PrivateAssets="all" />
              </ItemGroup>

            </Project>
            """;
        Directory.CreateDirectory(modFolder);
        File.WriteAllText(Path.Combine(modFolder, "Mod.cs"), "namespace Mod { public static class Mod { } }");
        string project = Path.Combine(modFolder, "Mod.csproj");
        File.WriteAllText(project, csproj);

        BuildResult result = Runner.Build(project);

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Reference("Assembly-CSharp.dll"), Does.StartWith(game.ManagedFolder), result.Output);
    }
}
