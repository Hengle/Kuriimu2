﻿using System;
using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser;

namespace Kompression.LempelZiv
{
    public class Mio0LE : BaseLz
    {
        protected override ILzEncoder CreateEncoder()
        {
            return new Mio0Encoder(ByteOrder.LittleEndian);
        }

        protected override ILzParser CreateParser(int inputLength)
        {
            return new PlusOneGreedyParser(new NeedleHaystackMatchFinder(3, 0x12, 0x1000));
        }

        protected override ILzDecoder CreateDecoder()
        {
            throw new NotImplementedException();
        }
    }
}
