﻿using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Queues
{
    internal interface IRelationQueue : IDisposable
    {
        bool Read();
        IRelationMetadata Metadata { get; }
    }
}