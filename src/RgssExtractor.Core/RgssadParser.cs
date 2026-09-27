using System.IO;

namespace RgssExtractor.Core
{
    internal class RgssadParser : Parser
    {
        public RgssadParser(BinaryReader file) : base(file)
        {
        }

        public string ReadFilename(int len)
        {
            byte[] array = inFile.ReadBytes(len);
            for (int i = 0; i < len; i++)
            {
                byte[] expr_18_cp_0 = array;
                int expr_18_cp_1 = i;
                expr_18_cp_0[expr_18_cp_1] ^= (byte)magicKey;
                magicKey = magicKey * 7 + 3;
            }

            return GetString(array);
        }

        public void ParseTable()
        {
            while (inFile.BaseStream.Position != inFile.BaseStream.Length)
            {
                int num = inFile.ReadInt32();
                num ^= magicKey;
                magicKey = magicKey * 7 + 3;
                string name = ReadFilename(num);
                long num2 = inFile.ReadInt32();
                num2 ^= magicKey;
                magicKey = magicKey * 7 + 3;
                long position = inFile.BaseStream.Position;
                inFile.BaseStream.Seek(num2, SeekOrigin.Current);
                Entry entry = new Entry();
                entry.Name = name;
                entry.Offset = position;
                entry.Size = num2;
                entry.DataKey = magicKey;
                entries.Add(entry);
            }
        }

        public override void ParseFile()
        {
            uint magickey = 3735931646u;
            magicKey = (int)magickey;
            ParseTable();
        }
    }
}