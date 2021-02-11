using System;
using System.Collections.Generic;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;

namespace Jerrycurl.Mvc.Projections
{
    public interface IProjectionData2
    {
        public IField Value { get; }
        public IField Input { get; }
        public IField Output { get; }
    }
}
