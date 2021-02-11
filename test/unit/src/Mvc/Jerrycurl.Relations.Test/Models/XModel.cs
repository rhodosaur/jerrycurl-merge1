﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Jerrycurl.Relations.Test.Models
{
    public class XModel
    {
        public List<int> Xs { get; set; }
        public List<int> Ys { get; set; }
        public List<NestedZ> Zs { get; set; }

        public class NestedZ
        {
            public List<int> Values { get; set; }
        }
    }
}
