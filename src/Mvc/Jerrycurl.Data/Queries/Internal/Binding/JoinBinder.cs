using System.Linq.Expressions;
using Jerrycurl.Data.Metadata;

namespace Jerrycurl.Data.Queries.Internal.Binding
{
    internal class JoinBinder : NodeBinder
    {
        public JoinBinder(IBindingMetadata metadata)
            : base(metadata)
        {

        }

        public ParameterExpression Array { get; set; }
        public int ArrayIndex { get; set; }
        public bool IsManyToOne { get; set; }
    }
}
