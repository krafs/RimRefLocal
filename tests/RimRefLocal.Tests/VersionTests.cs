using NUnit.Framework;

namespace RimRefLocal.Tests;

public class VersionTests
{
    // Before 1.7 each platform has its own build of the game, and RimRef has the 64-bit Windows one.
    [TestCase(GameLayout.Linux)]
    [TestCase(GameLayout.MacOS)]
    public void OtherPlatformsBuildBefore17_SameVersion_CompilesAgainstTheGame(GameLayout layout)
    {
        using var folder = new TemporaryFolder();
        FakeGame game = FakeGame.Create(folder.Path, RimRef.Version16, layout);
        game.ReplaceAssemblyCSharp();
        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net472</TargetFramework>
                <RimWorldDirectory>{game.Folder}</RimWorldDirectory>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Krafs.Rimworld.Ref" Version="{RimRef.Version16}" />
              </ItemGroup>

            </Project>
            """;
        string project = Mod.Create(Path.Combine(folder.Path, "Mod"), csproj);

        BuildResult result = Runner.Build(project);

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Reference("Assembly-CSharp.dll"), Does.StartWith(game.ManagedFolder), result.Output);
    }

    [Test]
    public void OtherPlatformsBuildBefore17_OtherVersion_KeepsRimRef()
    {
        using var folder = new TemporaryFolder();
        FakeGame game = FakeGame.Create(folder.Path, RimRef.Version16, GameLayout.Linux);
        game.ReplaceAssemblyCSharp();
        game.SetVersion("1.6.4850");
        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net472</TargetFramework>
                <RimWorldDirectory>{game.Folder}</RimWorldDirectory>
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
    }

    // From 1.7 every platform has the same build, but 1.7.4977 was released twice as two different builds.
    [Test]
    public void OtherBuild17_SameVersion_KeepsRimRef()
    {
        using var folder = new TemporaryFolder();
        FakeGame game = FakeGame.Create(folder.Path, RimRef.Version17, GameLayout.Root);
        game.ReplaceAssemblyCSharp();
        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <RimWorldDirectory>{game.Folder}</RimWorldDirectory>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Krafs.Rimworld.Ref" Version="{RimRef.Version17}" />
              </ItemGroup>

            </Project>
            """;
        string project = Mod.Create(Path.Combine(folder.Path, "Mod"), csproj);

        BuildResult result = Runner.Build(project);

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Reference("Assembly-CSharp.dll"), Does.StartWith(RimRef.Folder(RimRef.Version17)), result.Output);
    }

    [TestCase("1.7.5000", true)]
    [TestCase("1.8.5000", false)]
    public void NotStrict_ComparesMinorVersions(string gameVersion, bool expectGame)
    {
        using var folder = new TemporaryFolder();
        FakeGame game = FakeGame.Create(folder.Path, RimRef.Version17, GameLayout.Root);
        game.ReplaceAssemblyCSharp();
        game.SetVersion(gameVersion);
        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <RimWorldDirectory>{game.Folder}</RimWorldDirectory>
                <UseStrictRimWorldVersionComparison>false</UseStrictRimWorldVersionComparison>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Krafs.Rimworld.Ref" Version="{RimRef.Version17}" />
              </ItemGroup>

            </Project>
            """;
        string project = Mod.Create(Path.Combine(folder.Path, "Mod"), csproj);

        BuildResult result = Runner.Build(project);

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        string expectedFolder = expectGame ? game.ManagedFolder : RimRef.Folder(RimRef.Version17);
        Assert.That(result.Reference("Assembly-CSharp.dll"), Does.StartWith(expectedFolder), result.Output);
    }
}
