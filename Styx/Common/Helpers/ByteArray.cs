using System;
using System.Text;

namespace Styx.Common.Helpers
{
    public static class ByteArray
    {
        public static string Dump(byte[] array, int indent = 0)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            string prefix = new string('\t', indent);
            var builder = new StringBuilder();
            builder.AppendLine(prefix + "|---------------------------------------------------------------------------|");
            builder.AppendLine(prefix + "| OFFSET | 0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F  |0123456789ABCDEF|");
            builder.AppendLine(prefix + "|----------------------------------------------------------|----------------|");
            int rows = (array.Length + 15) / 16;
            for (int i = 0; i < rows; i++)
            {
                builder.AppendFormat(prefix + "|{0:X8}| ", i * 16);
                int start = i * 16;
                int end = Math.Min(start + 16, array.Length);
                for (int j = start; j < end; j++)
                    builder.AppendFormat("{0:X2} ", array[j]);

                int padding = 16 - (end - start);
                for (int j = 0; j < padding; j++)
                    builder.Append("-- ");

                builder.Append("|");
                for (int j = start; j < end; j++)
                {
                    char value = (char)array[j];
                    builder.Append(char.IsControl(value) ? '.' : value);
                }
                for (int j = 0; j < padding; j++)
                    builder.Append('.');
                builder.AppendLine("|");
            }

            builder.Append(prefix + "|----------------------------------------------------------|----------------|");
            return builder.ToString();
        }
    }
}
