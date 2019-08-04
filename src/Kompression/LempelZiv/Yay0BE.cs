﻿using System;
using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser;

namespace Kompression.LempelZiv
{
    public class Yay0BE : BaseLz
    {
        protected override ILzEncoder CreateEncoder()
        {
            return new Yay0Encoder(ByteOrder.BigEndian);
        }

        protected override ILzParser CreateParser(int inputLength)
        {
            return new PlusOneGreedyParser(new NeedleHaystackMatchFinder(3, 0x111, 0x1000));
        }

        protected override ILzDecoder CreateDecoder()
        {
            throw new NotImplementedException();
        }
    }
}
