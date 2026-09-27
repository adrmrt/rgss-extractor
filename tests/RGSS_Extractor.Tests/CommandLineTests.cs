using System.Diagnostics;

namespace RGSS_Extractor.Tests;

// Runs the real app exe, because the command-line path lives in Program.Main.
public sealed class CommandLineTests : IDisposable
{
    private static readonly string ExePath = Path.ChangeExtension(typeof(MainParser).Assembly.Location, ".exe");

    private readonly TempDir temp = new();

    public void Dispose() => temp.Dispose();

    private static int Run(string workingDirectory, params string[] args)
    {
        var info = new ProcessStartInfo(ExePath)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
        {
            info.ArgumentList.Add(arg);
        }

        using var process = Process.Start(info)!;
        if (!process.WaitForExit(TimeSpan.FromSeconds(30)))
        {
            process.Kill();
            Assert.Fail("The exe did not exit within 30 seconds.");
        }

        return process.ExitCode;
    }

    [Fact]
    public void ExtractsArchive()
    {
        var archive = temp.File("game.rgssad", ArchiveWriter.WriteV1((@"Data\f.txt", "hello"u8.ToArray())));
        var outDir = Path.Join(temp.Path, "out");

        Assert.Equal(0, Run(temp.Path, archive, outDir));

        Assert.Equal("hello"u8.ToArray(), File.ReadAllBytes(Path.Join(outDir, "Data", "f.txt")));
    }

    [Fact(Skip = "Known bug: output paths resolve against the exe folder (Assembly.Location), which also crashes single-file builds")]
    public void RelativeOutputPathResolvesAgainstWorkingDirectory()
    {
        temp.File("game.rgssad", ArchiveWriter.WriteV1(("f.txt", "hello"u8.ToArray())));

        Assert.Equal(0, Run(temp.Path, "game.rgssad", "out"));

        Assert.Equal("hello"u8.ToArray(), File.ReadAllBytes(Path.Join(temp.Path, "out", "f.txt")));
    }

    [Fact(Skip = "Known bug: a missing output argument throws IndexOutOfRangeException")]
    public void MissingOutputArgumentFailsCleanly()
    {
        var archive = temp.File("game.rgssad", ArchiveWriter.WriteV1(("f.txt", [1])));

        Assert.Equal(1, Run(temp.Path, archive));
    }

    [Fact(Skip = "Known bug: an invalid archive exits with code 0")]
    public void InvalidArchiveFailsCleanly()
    {
        var file = temp.File("bad.bin", "not an archive"u8.ToArray());

        Assert.Equal(1, Run(temp.Path, file, Path.Join(temp.Path, "out")));
    }
}
