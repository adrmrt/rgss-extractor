using System.IO;

namespace RgssExtractor.Core
{
    internal class Rgss3aParser : Parser
    {
        public Rgss3aParser(BinaryReader file) : base(file)
        {
        }

        public string ReadFilename(int len)
        {
            byte[] array = inFile.ReadBytes(len);
            for (int i = 0; i < len; i++)
            {
                byte[] expr_18_cp_0 = array;
                int expr_18_cp_1 = i;
                expr_18_cp_0[expr_18_cp_1] ^= (byte)(magicKey >> 8 * (i % 4));
            }

            return GetString(array);
        }

        public void ParseTable()
        {
            while (true)
            {
                long num = inFile.ReadInt32();
                num ^= magicKey;
                if (num == 0L)
                {
                    break;
                }

                long num2 = inFile.ReadInt32();
                int num3 = inFile.ReadInt32();
                int num4 = inFile.ReadInt32();
                num2 ^= magicKey;
                num3 ^= magicKey;
                num4 ^= magicKey;
                string name = ReadFilename(num4);
                Entry entry = new Entry();
                entry.Offset = num;
                entry.Name = name;
                entry.Size = num2;
                entry.DataKey = num3;
                entries.Add(entry);
            }
        }

        public override void ParseFile()
        {
            magicKey = inFile.ReadInt32() * 9 + 3;
            ParseTable();
        }
    }
}