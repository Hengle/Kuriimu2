﻿using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;
using Kontract.Models.IO;

namespace Kompression.Implementations.Encoders
{
    // TODO: Test this compression thoroughly
    public class Lz77Encoder : IEncoder
    {
        private readonly IMatchParser _matchParser;

        public Lz77Encoder(IMatchParser matchParser)
        {
            _matchParser = matchParser;
        }

        public void Encode(Stream input, Stream output)
        {
            var matches = _matchParser.ParseMatches(input).ToArray();
            WriteCompressedData(input, output, matches);
        }

        private void WriteCompressedData(Stream input, Stream output, Match[] matches)
        {
            using var bw = new BitWriter(output, BitOrder.LeastSignificantBitFirst, 1, ByteOrder.BigEndian);

            foreach (var match in matches)
            {
                while (input.Position < match.Position)
                {
                    bw.WriteBit(0);
                    bw.WriteByte((byte)input.ReadByte());
                }

                bw.WriteBit(1);
                bw.WriteByte((byte)match.Displacement);
                bw.WriteByte(match.Length);

                input.Position += match.Length;
                bw.WriteByte(input.ReadByte());
            }

            while (input.Position < input.Length)
            {
                bw.WriteBit(0);
                bw.WriteByte((byte)input.ReadByte());
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
