﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Jerrycurl.Relations.Internal.V11.Enumerators
{
    internal class SourceItem : NameBuffer
    {
        public SourceItem(IField2 source)
            : base(source.Identity.Name, new DotNotation2())
        {

        }
    }
}
