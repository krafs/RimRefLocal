using NUnit.Framework;

namespace RimRefLocal.Tests;

public class SwapTests
{
    [TestCase(RimRef.Version17, "net10.0", GameLayout.Root)]
    [TestCase(RimRef.Version16, "net472", GameLayout.Windows)]
    public void SameBuild_CompilesAgainstTheGame(string rimRefVersion, string targetFramework, GameLayout layout)
    {
        using var folder = new TemporaryFolder();
        FakeGame game = FakeGame.Create(folder.Path, rimRefVersion, layout);
        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>{targetFramework}</TargetFramework>
                <RimWorldDirectory>{game.Folder}</RimWorldDirectory>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Krafs.Rimworld.Ref" Version="{rimRefVersion}" />
              </ItemGroup>

            </Project>
            """;
        string project = Mod.Create(Path.Combine(folder.Path, "Mod"), csproj);

        BuildResult result = Runner.Build(project);

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Reference("Assembly-CSharp.dll"), Does.StartWith(game.ManagedFolder), result.Output);
        Assert.That(result.ReferencePaths, Has.None.StartWith(RimRef.Folder(rimRefVersion)), result.Output);
    }

    // Before 1.7 the game ships its own .NET, which RimRef has copies of. Modders want the game's, to see what it really has.
    [Test]
    public void SameBuildBefore17_CompilesAgainstTheGamesMscorlib()
    {
        using var folder = new TemporaryFolder();
        FakeGame game = FakeGame.Create(folder.Path, RimRef.Version16, GameLayout.Windows);
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
        Assert.That(result.Reference("mscorlib.dll"), Does.StartWith(game.ManagedFolder), result.Output);
        Assert.That(result.Reference("System.Core.dll"), Does.StartWith(game.ManagedFolder), result.Output);
    }

    // A mod that ships the game's assemblies breaks the game.
    [Test]
    public void SameBuild_DoesNotCopyTheGameToTheOutput()
    {
        using var folder = new TemporaryFolder();
        FakeGame game = FakeGame.Create(folder.Path, RimRef.Version16, GameLayout.Windows);
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
        string modFolder = Path.Combine(folder.Path, "Mod");
        string project = Mod.Create(modFolder, csproj);

        BuildResult result = Runner.Build(project);

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        string outputFolder = Path.Combine(modFolder, "bin");
        string[] outputFiles = Directory.GetFiles(outputFolder, "*", SearchOption.AllDirectories);
        string[] outputFileNames = [.. outputFiles.Select(Path.GetFileName)!];
        Assert.That(outputFileNames, Is.EquivalentTo(new[] { "Mod.dll", "Mod.pdb" }), result.Output);
    }

    // From 1.7 the game folder also has Ludeon's own build of .NET. RimRef doesn't have it, so it's never swapped in.
    [Test]
    public void SameBuild17_LeavesTheGamesDotNetAlone()
    {
        using var folder = new TemporaryFolder();
        FakeGame game = FakeGame.Create(folder.Path, RimRef.Version17, GameLayout.Root);
        string anyAssembly = Path.Combine(game.ManagedFolder, "Assembly-CSharp.dll");
        File.Copy(anyAssembly, Path.Combine(game.ManagedFolder, "System.Runtime.dll"));
        File.Copy(anyAssembly, Path.Combine(game.ManagedFolder, "System.Private.CoreLib.dll"));
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
        Assert.That(result.Reference("Assembly-CSharp.dll"), Does.StartWith(game.ManagedFolder), result.Output);
        Assert.That(result.Reference("System.Runtime.dll"), Does.Not.StartWith(game.ManagedFolder), result.Output);
        Assert.That(result.ReferencePaths.Select(Path.GetFileName), Has.No.Member("System.Private.CoreLib.dll"), result.Output);
    }

    [Test]
    public void Release_KeepsRimRef()
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

        BuildResult result = Runner.Build(project, "-p:Configuration=Release");

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Reference("Assembly-CSharp.dll"), Does.StartWith(RimRef.Folder(RimRef.Version17)), result.Output);
    }

    [Test]
    public void UseLocalRimWorldAssembliesFalse_KeepsRimRef()
    {
        using var folder = new TemporaryFolder();
        FakeGame game = FakeGame.Create(folder.Path, RimRef.Version17, GameLayout.Root);
        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <RimWorldDirectory>{game.Folder}</RimWorldDirectory>
                <UseLocalRimWorldAssemblies>false</UseLocalRimWorldAssemblies>
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
}
