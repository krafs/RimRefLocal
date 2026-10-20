namespace RimRefLocal.Tests;

internal sealed class TemporaryFolder : IDisposable
{
    internal TemporaryFolder()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RimRefLocal.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        Path = path;
    }

    internal string Path { get; }

    public void Dispose()
    {
        Directory.Delete(Path, recursive: true);
    }
}
