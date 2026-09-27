using System.Text;

namespace RGSS_Extractor.Tests;

public sealed class MainParserTests : IDisposable
{
    private readonly TempDir temp = new();
    private readonly MainParser parser = new();

    public void Dispose()
    {
#pragma warning disable CS0612 // CloseFile is the only way to release the archive handle
        parser.CloseFile();
#pragma warning restore CS0612
        temp.Dispose();
    }

    private List<Entry> Open(byte[] archive) => parser.ParseFile(temp.File("archive.bin", archive));

    private Dictionary<string, byte[]> ReadAll(List<Entry> entries) =>
        entries.ToDictionary(e => e.Name, parser.GetFileData);

    // Golden archives: bytes computed by hand from the format spec, independent of both parser and ArchiveWriter.

    [Fact]
    public void ParsesGoldenV1Archive()
    {
        byte[] archive =
        [
            0x52, 0x47, 0x53, 0x53, 0x41, 0x44, 0x00, 0x01, 0xFF, 0xCA, 0xAD, 0xDE, 0x94,
            0xB3, 0xDA, 0x43, 0x9F, 0x95, 0x9F, 0xB6, 0x36, 0x81,
        ];

        var entry = Assert.Single(Open(archive));

        Assert.Equal("a", entry.Name);
        Assert.Equal("hello"u8.ToArray(), parser.GetFileData(entry));
    }

    [Fact]
    public void ParsesGoldenV3Archive()
    {
        byte[] archive =
        [
            0x52, 0x47, 0x53, 0x53, 0x41, 0x44, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x22,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x07, 0x03, 0x02, 0x01, 0x02, 0x00,
            0x00, 0x00, 0x62, 0x03, 0x00, 0x00, 0x00, 0x6C, 0x6A,
        ];

        var entry = Assert.Single(Open(archive));

        Assert.Equal("a", entry.Name);
        Assert.Equal("hi"u8.ToArray(), parser.GetFileData(entry));
    }

    public static TheoryData<string, byte[]> RejectedFiles => new()
    {
        { "empty file", [] },
        { "wrong magic", "NOTRGS\0\u0001"u8.ToArray() },
        { "unknown version 2", "RGSSAD\0\u0002"u8.ToArray() },
    };

    [Theory]
    [MemberData(nameof(RejectedFiles))]
    public void RejectsNonArchives(string _, byte[] contents)
    {
        Assert.Null(Open(contents));
    }

    private static readonly (string Name, byte[] Data)[] SampleFiles =
    [
        (@"Data\Scripts.rvdata2", Encoding.UTF8.GetBytes("scripts go here")),
        (@"Graphics\Pictures\title.png", Enumerable.Range(0, 1000).Select(i => (byte)i).ToArray()),
        (@"Audio\SE\" + new string([(char)0x30C6, (char)0x30B9, (char)0x30C8]) + ".ogg", [1, 2, 3]),
        ("empty.txt", []),
    ];

    [Fact]
    public void RoundTripsV1Archive()
    {
        var entries = Open(ArchiveWriter.WriteV1(SampleFiles));

        Assert.Equal(SampleFiles.Select(f => f.Name), entries.Select(e => e.Name));
        var contents = ReadAll(entries);
        foreach (var (name, data) in SampleFiles)
        {
            Assert.Equal(data, contents[name]);
        }
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(0x12345678u)]
    [InlineData(0xFFFFFFFFu)] // negative magic key as int
    public void RoundTripsV3Archive(uint seed)
    {
        var files = SampleFiles.Select((f, i) => (f.Name, f.Data, FileKey: 0x9E3779B9u * (uint)(i + 1))).ToArray();

        var entries = Open(ArchiveWriter.WriteV3(seed, files));

        Assert.Equal(files.Select(f => f.Name), entries.Select(e => e.Name));
        var contents = ReadAll(entries);
        foreach (var (name, data, _) in files)
        {
            Assert.Equal(data, contents[name]);
        }
    }

    // Sizes around the 4-byte word boundary exercise the trailing-byte decryption path.
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(4099)]
    public void DecryptsAnyPayloadSize(int size)
    {
        var data = Enumerable.Range(0, size).Select(i => (byte)(i * 31 + 7)).ToArray();

        var v1 = Open(ArchiveWriter.WriteV1(("f", data)));
        Assert.Equal(data, parser.GetFileData(v1[0]));

        var v3 = new MainParser();
        var entries = v3.ParseFile(temp.File("v3.bin", ArchiveWriter.WriteV3(42, ("f", data, 0xCAFEBABEu))));
        Assert.Equal(data, v3.GetFileData(entries[0]));
#pragma warning disable CS0612
        v3.CloseFile();
#pragma warning restore CS0612
    }

    [Fact]
    public void ExportArchiveWritesAllFilesUnderOutputFolder()
    {
        Open(ArchiveWriter.WriteV1(SampleFiles));
        var outDir = Path.Join(temp.Path, "out");

        parser.ExportArchive(outDir);

        foreach (var (name, data) in SampleFiles)
        {
            Assert.Equal(data, File.ReadAllBytes(Path.Join(outDir, name)));
        }
    }

    [Fact]
    public void ExportFileWritesSingleEntry()
    {
        var entries = Open(ArchiveWriter.WriteV3(7, SampleFiles.Select(f => (f.Name, f.Data, 1u)).ToArray()));
        var outDir = Path.Join(temp.Path, "out");

        parser.ExportFile(entries[1], outDir);

        Assert.Equal(SampleFiles[1].Data, File.ReadAllBytes(Path.Join(outDir, SampleFiles[1].Name)));
        Assert.False(File.Exists(Path.Join(outDir, SampleFiles[0].Name)));
    }

    // Regression tests for bugs found in the initial audit.

    [Fact]
    public void ExportOverwritesLargerExistingFile()
    {
        Open(ArchiveWriter.WriteV1(("f.txt", "new"u8.ToArray())));
        var outDir = Path.Join(temp.Path, "out");
        Directory.CreateDirectory(outDir);
        File.WriteAllBytes(Path.Join(outDir, "f.txt"), "much older content"u8.ToArray());

        parser.ExportArchive(outDir);

        Assert.Equal("new"u8.ToArray(), File.ReadAllBytes(Path.Join(outDir, "f.txt")));
    }

    [Fact]
    public void ExportKeepsEntriesInsideOutputFolder()
    {
        Open(ArchiveWriter.WriteV1((@"..\escaped.txt", [1])));
        var outDir = Path.Join(temp.Path, "out");

        // Rejecting the entry (throwing) or skipping it are both acceptable; escaping is not.
        Record.Exception(() => parser.ExportArchive(outDir));

        Assert.False(File.Exists(Path.Join(temp.Path, "escaped.txt")));
    }

    [Fact]
    public void ExportAfterFailedOpenDoesNothing()
    {
        Open(ArchiveWriter.WriteV1(("f.txt", [1])));
#pragma warning disable CS0612
        parser.CloseFile(); // what MainForm does before opening the next archive
#pragma warning restore CS0612
        Assert.Null(parser.ParseFile(temp.File("bad.bin", "not an archive"u8.ToArray())));
        var outDir = Path.Join(temp.Path, "out");

        parser.ExportArchive(outDir);

        Assert.False(Directory.Exists(outDir));
    }

    [Fact]
    public void RejectedFileIsNotLeftOpen()
    {
        var path = temp.File("bad.bin", "not an archive"u8.ToArray());

        Assert.Null(parser.ParseFile(path));

        File.Delete(path); // throws IOException on Windows while a handle is open
    }

    [Fact]
    public void TruncatedArchiveThrowsAndIsNotLeftOpen()
    {
        var archive = ArchiveWriter.WriteV3(1, ("f.txt", [1, 2, 3], 5u));
        var path = temp.File("truncated.rgss3a", archive[..20]); // cut off inside the file table

        Assert.ThrowsAny<IOException>(() => parser.ParseFile(path));

        File.Delete(path);
    }
}
