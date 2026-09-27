using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RgssExtractor.Core;

public class MainParser
{
    private Parser parser;

    public List<Entry> ParseFile(string path)
    {
        parser?.CloseFile();
        parser = null;

        BinaryReader binaryReader = new BinaryReader(File.OpenRead(path));
        try
        {
            string @string = Encoding.UTF8.GetString(binaryReader.ReadBytes(6));
            if (@string != "RGSSAD")
            {
                return null;
            }

            binaryReader.ReadByte();
            int version = binaryReader.ReadByte();
            Parser candidate = CreateParser(version, binaryReader);
            if (candidate == null)
            {
                return null;
            }

            candidate.ParseFile();
            parser = candidate;
            return parser.entries;
        }
        finally
        {
            // Rejected or unparsable files must not stay open.
            if (parser == null)
            {
                binaryReader.Close();
            }
        }
    }

    public byte[] GetFileData(Entry entry)
    {
        return parser.ReadData(entry.Offset, entry.Size, entry.DataKey);
    }

    public void ExportFile(Entry entry, string path)
    {
        parser.WriteFile(entry, path);
    }

    public void ExportArchive(string path)
    {
        if (parser == null)
        {
            return;
        }

        parser.WriteEntries(path);
    }

    public void CloseFile()
    {
        parser?.CloseFile();
    }

    private static Parser CreateParser(int version, BinaryReader inFile)
    {
        if (version == 1)
        {
            return new RgssadParser(inFile);
        }

        if (version == 3)
        {
            return new Rgss3aParser(inFile);
        }

        return null;
    }
}