﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Encoding.Models;
using Komponent.IO;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the RGBA encoding.
    /// </summary>
    public class RGBA : IColorEncoding
    {
        private readonly ByteOrder _byteOrder;

        private readonly RgbaPixelDescriptor _descriptor;
        private Func<BinaryReaderX, long> _readValueDelegate;
        private Action<BinaryWriterX, long> _writeValueDelegate;

        /// <inheritdoc />
        public int BitDepth { get; }

        /// <inheritdoc />
        public bool IsBlockCompression => false;

        /// <inheritdoc />
        public string FormatName { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RGBA"/>.
        /// </summary>
        /// <param name="r">Value of the red component.</param>
        /// <param name="g">Value of the green component.</param>
        /// <param name="b">Value of the blue component.</param>
        /// <param name="componentOrder">The order of the color components.</param>
        /// <param name="byteOrder">The byte order in which atomic values are read.</param>
        public RGBA(int r, int g, int b, string componentOrder = "RGBA", ByteOrder byteOrder = ByteOrder.LittleEndian) :
            this(r, g, b, 0, componentOrder, byteOrder)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RGBA"/>.
        /// </summary>
        /// <param name="r">Value of the red component.</param>
        /// <param name="g">Value of the green component.</param>
        /// <param name="b">Value of the blue component.</param>
        /// <param name="a">Value of the alpha component.</param>
        /// <param name="componentOrder">The order of the color components.</param>
        /// <param name="byteOrder">The byte order in which atomic values are read.</param>
        public RGBA(int r, int g, int b, int a, string componentOrder = "RGBA", ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            _descriptor = new RgbaPixelDescriptor(componentOrder, r, g, b, a);
            _byteOrder = byteOrder;

            var bitDepth = r + g + b + a;
            SetValueDelegates(bitDepth);

            BitDepth = bitDepth;
            FormatName = _descriptor.GetPixelName();
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using var br = new BinaryReaderX(new MemoryStream(tex), _byteOrder);

            while (br.BaseStream.Position < br.BaseStream.Length)
                yield return _descriptor.GetColor(_readValueDelegate(br));
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var ms = new MemoryStream();
            using var bw = new BinaryWriterX(ms, _byteOrder);

            foreach (var color in colors)
                _writeValueDelegate(bw, _descriptor.GetValue(color));

            return ms.ToArray();
        }

        private void SetValueDelegates(int bitDepth)
        {
            var bytesToRead = bitDepth / 8 + (bitDepth % 8 > 0 ? 1 : 0);

            if (bitDepth == 4)
            {
                _readValueDelegate = br => br.ReadNibble();
                _writeValueDelegate = (bw, value) => bw.WriteNibble((int)value);
                return;
            }

            switch (bytesToRead)
            {
                case 1:
                    _readValueDelegate = br => br.ReadByte();
                    _writeValueDelegate = (bw, value) => bw.Write((byte)value);
                    break;

                case 2:
                    _readValueDelegate = br => br.ReadUInt16();
                    _writeValueDelegate = (bw, value) => bw.Write((ushort)value);
                    break;

                case 3:
                    _readValueDelegate = br =>
                    {
                        var bytes = br.ReadBytes(3);
                        return (bytes[0] << 16) | (bytes[1] << 8) | bytes[2];
                    };
                    _writeValueDelegate = (bw, value) =>
                    {
                        var bytes = new[] { (byte)(value >> 16), (byte)(value >> 8), (byte)value };
                        bw.Write(bytes);
                    };
                    break;

                case 4:
                    _readValueDelegate = br => br.ReadUInt32();
                    _writeValueDelegate = (bw, value) => bw.Write((uint)value);
                    break;
            }
        }
    }
}
