﻿using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Data.Metadata;

namespace Jerrycurl.Data.Queries.Internal.V11.Binders
{
    internal abstract class NodeReader
    {
        public IBindingMetadata Metadata { get; }
    }
}
