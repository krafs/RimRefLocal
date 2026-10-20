using System.Diagnostics;
using System.Text.Json;

namespace RimRefLocal.Tests;

internal sealed record ProcessResult(int ExitCode, string Output);

internal sealed record BuildResult(int ExitCode, string Output, IReadOnlyList<string> ReferencePaths)
{
    // The path of the reference the compiler got for this file.
    internal string Reference(string fileName)
    {
        return ReferencePaths.Single(path => Path.GetFileName(path).Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }
}

internal static class Runner
{
    internal static BuildResult Build(string projectPath, params string[] arguments)
    {
        var environment = new Dictionary<string, string>();
        return Build(projectPath, environment, arguments);
    }

    // Builds and lists the references the compiler got. -getItem only sees what the targets did when a target is named.
    internal static BuildResult Build(string projectPath, Dictionary<string, string> environment, params string[] arguments)
    {
        bool namesTarget = arguments.Any(argument => argument.StartsWith("-t:", StringComparison.Ordinal));
        string[] target = namesTarget ? [] : ["-t:Build"];
        string[] buildArguments = ["msbuild", projectPath, "-restore", "-nodeReuse:false", "-getItem:ReferencePath", .. target, .. arguments];

        ProcessResult result = Run("dotnet", environment, buildArguments);
        List<string> referencePaths = result.ExitCode == 0 ? ParseReferencePaths(result.Output) : [];

        return new BuildResult(result.ExitCode, result.Output, referencePaths);
    }

    internal static ProcessResult Run(string command, params string[] arguments)
    {
        var environment = new Dictionary<string, string>();
        return Run(command, environment, arguments);
    }

    internal static ProcessResult Run(string command, Dictionary<string, string> environment, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(command)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // You may have set this for your own mods, and it would win over the tests' fake games.
        startInfo.Environment.Remove("RimWorldDirectory");
        foreach (var (name, value) in environment)
        {
            startInfo.Environment[name] = value;
        }

        using Process process = Process.Start(startInfo)!;

        // Read both streams before waiting, so a full pipe can't block the build while we block on it.
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        string output = outputTask.Result + errorTask.Result;
        return new ProcessResult(process.ExitCode, output);
    }

    // With -getItem, MSBuild prints the items as JSON, after anything restore prints.
    private static List<string> ParseReferencePaths(string output)
    {
        int jsonStart = output.IndexOf('{', StringComparison.Ordinal);
        string json = output[jsonStart..];
        using var document = JsonDocument.Parse(json);
        var items = document.RootElement.GetProperty("Items").GetProperty("ReferencePath").EnumerateArray();

        List<string> paths = [];
        foreach (JsonElement item in items)
        {
            string path = item.GetProperty("FullPath").GetString()!;
            paths.Add(path);
        }

        return paths;
    }
}
