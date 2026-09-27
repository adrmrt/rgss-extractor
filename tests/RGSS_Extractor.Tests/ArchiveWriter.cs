using System.Text;

namespace RGSS_Extractor.Tests;

// Test-only encoder, written from the archive format rather than from the parser code,
// so round-trip tests do not just mirror the parser's own assumptions.
internal static class ArchiveWriter
{
    private static readonly byte[] Magic = "RGSSAD\0"u8.ToArray();

    // Format version 1: used by .rgssad (XP) and .rgss2a (VX).
    public static byte[] WriteV1(params (string Name, byte[] Data)[] files)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(Magic);
        w.Write((byte)1);

        uint key = 0xDEADCAFE;
        foreach (var (name, data) in files)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name);
            w.Write((uint)nameBytes.Length ^ key);
            key = Advance(key);
            foreach (var b in nameBytes)
            {
                w.Write((byte)(b ^ (byte)key));
                key = Advance(key);
            }

            w.Write((uint)data.Length ^ key);
            key = Advance(key);
            w.Write(Crypt(data, key));
        }

        return ms.ToArray();
    }

    // Format version 3: used by .rgss3a (VX Ace).
    public static byte[] WriteV3(uint seed, params (string Name, byte[] Data, uint FileKey)[] files)
    {
        uint key = seed * 9 + 3;
        var names = files.Select(f => Encoding.UTF8.GetBytes(f.Name)).ToArray();

        // Header (8) + seed (4) + per entry (16 + name) + terminator (4).
        long offset = 12 + names.Sum(n => 16 + n.Length) + 4;

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(Magic);
        w.Write((byte)3);
        w.Write(seed);

        for (int i = 0; i < files.Length; i++)
        {
            w.Write((uint)offset ^ key);
            w.Write((uint)files[i].Data.Length ^ key);
            w.Write(files[i].FileKey ^ key);
            w.Write((uint)names[i].Length ^ key);
            for (int j = 0; j < names[i].Length; j++)
            {
                w.Write((byte)(names[i][j] ^ (byte)(key >> 8 * (j % 4))));
            }

            offset += files[i].Data.Length;
        }

        w.Write(key); // offset 0 after XOR ends the table

        foreach (var (_, data, fileKey) in files)
        {
            w.Write(Crypt(data, fileKey));
        }

        return ms.ToArray();
    }

    // XOR each byte with the matching byte of the little-endian key; the key advances after every 4 bytes.
    public static byte[] Crypt(byte[] data, uint key)
    {
        var result = (byte[])data.Clone();
        for (int i = 0; i < result.Length; i++)
        {
            result[i] ^= (byte)(key >> 8 * (i % 4));
            if (i % 4 == 3)
            {
                key = Advance(key);
            }
        }

        return result;
    }

    private static uint Advance(uint key) => key * 7 + 3;
}
