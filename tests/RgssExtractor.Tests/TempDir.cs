namespace RgssExtractor.Tests;

internal sealed class TempDir : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("rgss-tests-").FullName;

    public string File(string name, byte[] contents)
    {
        var path = System.IO.Path.Join(Path, name);
        System.IO.File.WriteAllBytes(path, contents);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // The parser leaks its file handle on some paths; leftover temp files are harmless.
        }
    }
}
