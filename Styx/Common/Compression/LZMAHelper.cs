using System;
using System.IO;

namespace Styx.Common.Compression
{
    internal static class LZMAHelper
    {
        private const int dictionary = 1 << 23;

        // static Int32 posStateBits = 2;
        // static  Int32 litContextBits = 3; // for normal files
        // UInt32 litContextBits = 0; // for 32-bit data
        // static  Int32 litPosBits = 0;
        // UInt32 litPosBits = 2; // for 32-bit data
        // static   Int32 algorithm = 2;
        // static    Int32 numFastBytes = 128;

        // these are the default properties, keeping it simple for now:
        private static readonly object[] properties =
            {
                (dictionary),
                (2),
                (3),
                (0),
                (2),
                (128),
                "bt4",
                false
            };

        private static readonly CoderPropID[] propIDs =
            {
                CoderPropID.DictionarySize,
                CoderPropID.PosStateBits,
                CoderPropID.LitContextBits,
                CoderPropID.LitPosBits,
                CoderPropID.Algorithm,
                CoderPropID.NumFastBytes,
                CoderPropID.MatchFinder,
                CoderPropID.EndMarker
            };

		public static byte[] Compress(byte[] data)
		{
			using (var ms = new MemoryStream(data))
				return Compress(ms);
		}

        public static byte[] Compress(Stream stream)
        {
			var inStream = stream;
            var outStream = new MemoryStream();
            var encoder = new Encoder();
            encoder.SetCoderProperties(propIDs, properties);
            encoder.WriteCoderProperties(outStream);
            long fileSize = inStream.Length;
            for (int i = 0; i < 8; i++)
                outStream.WriteByte((Byte) (fileSize >> (8*i)));
            encoder.Code(inStream, outStream, -1, -1, null);
            return outStream.ToArray();
        }

		public static byte[] Decompress(byte[] data)
		{
			using (var ms = new MemoryStream(data))
				return Decompress(ms);
		}

        public static byte[] Decompress(Stream stream)
        {
			var newInStream = stream;

            var decoder = new Decoder();

            newInStream.Seek(0, 0);
            var newOutStream = new MemoryStream();

            var properties2 = new byte[5];
            if (newInStream.Read(properties2, 0, 5) != 5)
                throw (new Exception("input .lzma is too short"));
            long outSize = 0;
            for (int i = 0; i < 8; i++)
            {
                int v = newInStream.ReadByte();
                if (v < 0)
                    throw (new Exception("Can't Read 1"));
                outSize |= ((long) (byte) v) << (8*i);
            }
            decoder.SetDecoderProperties(properties2);

            long compressedSize = newInStream.Length - newInStream.Position;
            decoder.Code(newInStream, newOutStream, compressedSize, outSize, null);

            byte[] b = newOutStream.ToArray();

            return b;
        }
    }
}