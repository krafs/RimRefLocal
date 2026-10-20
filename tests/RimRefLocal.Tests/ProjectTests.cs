using NUnit.Framework;

namespace RimRefLocal.Tests;

public class ProjectTests
{
    // As from a shared library in the same solution.
    [Test]
    public void RimRefThroughProjectReference_CompilesAgainstTheGame()
    {
        using var folder = new TemporaryFolder();
        FakeGame game = FakeGame.Create(folder.Path, RimRef.Version17, GameLayout.Root);
        string libraryFolder = Path.Combine(folder.Path, "Library");
        string libraryProject = Path.Combine(libraryFolder, "Library.csproj");
        string libraryCsproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Krafs.Rimworld.Ref" Version="{RimRef.Version17}" />
              </ItemGroup>

            </Project>
            """;
        Directory.CreateDirectory(libraryFolder);
        File.WriteAllText(libraryProject, libraryCsproj);
        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <RimWorldDirectory>{game.Folder}</RimWorldDirectory>
              </PropertyGroup>

              <ItemGroup>
                <ProjectReference Include="{libraryProject}" />
              </ItemGroup>

            </Project>
            """;
        string project = Mod.Create(Path.Combine(folder.Path, "Mod"), csproj);

        BuildResult result = Runner.Build(project);

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Reference("Assembly-CSharp.dll"), Does.StartWith(game.ManagedFolder), result.Output);
    }

    // A mod for 1.6 and 1.7, with RimWorld 1.7 installed.
    [Test]
    public void MultiTargeting_SwapsOnlyTheMatchingFramework()
    {
        using var folder = new TemporaryFolder();
        FakeGame game = FakeGame.Create(folder.Path, RimRef.Version17, GameLayout.Root);
        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFrameworks>net472;net10.0</TargetFrameworks>
                <RimWorldDirectory>{game.Folder}</RimWorldDirectory>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Krafs.Rimworld.Ref" Version="{RimRef.Version16}" Condition="'$(TargetFramework)' == 'net472'" />
                <PackageReference Include="Krafs.Rimworld.Ref" Version="{RimRef.Version17}" Condition="'$(TargetFramework)' == 'net10.0'" />
              </ItemGroup>

            </Project>
            """;
        string project = Mod.Create(Path.Combine(folder.Path, "Mod"), csproj);

        BuildResult net472 = Runner.Build(project, "-p:TargetFramework=net472");
        BuildResult net10 = Runner.Build(project, "-p:TargetFramework=net10.0");

        Assert.That(net472.ExitCode, Is.Zero, net472.Output);
        Assert.That(net472.Reference("Assembly-CSharp.dll"), Does.StartWith(RimRef.Folder(RimRef.Version16)), net472.Output);
        Assert.That(net10.ExitCode, Is.Zero, net10.Output);
        Assert.That(net10.Reference("Assembly-CSharp.dll"), Does.StartWith(game.ManagedFolder), net10.Output);
    }

    // A Directory.Build.targets also applies to the projects in a solution that aren't mods.
    [Test]
    public void ProjectWithoutRimRef_BuildsUnchanged()
    {
        using var folder = new TemporaryFolder();
        FakeGame game = FakeGame.Create(folder.Path, RimRef.Version17, GameLayout.Root);
        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <RimWorldDirectory>{game.Folder}</RimWorldDirectory>
              </PropertyGroup>

            </Project>
            """;
        string project = Mod.Create(Path.Combine(folder.Path, "Tool"), csproj);

        BuildResult result = Runner.Build(project);

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.ReferencePaths, Has.None.StartWith(game.Folder), result.Output);
    }

    // IDEs get references from design-time builds, which resolve them without compiling.
    [Test]
    public void DesignTimeBuild_ResolvesTheGame()
    {
        using var folder = new TemporaryFolder();
        FakeGame game = FakeGame.Create(folder.Path, RimRef.Version17, GameLayout.Root);
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

        BuildResult result = Runner.Build(project, "-t:ResolveAssemblyReferencesDesignTime;ResolveReferences", "-p:DesignTimeBuild=true", "-p:SkipCompilerExecution=true", "-p:ProvideCommandLineArgs=true");

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Reference("Assembly-CSharp.dll"), Does.StartWith(game.ManagedFolder), result.Output);
    }
}
