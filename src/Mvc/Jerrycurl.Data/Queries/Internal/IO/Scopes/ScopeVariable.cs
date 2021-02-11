using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    public class ScopeVariable
    {
        public string Name { get; set; }
        public Type Type { get; set; }

        public ScopeVariable(string name, Type type)
        {
            this.Name = name;
            this.Type = type;
        }

        public virtual ParameterExpression Build()
            => Expression.Variable(this.Type, this.Name);
    }
}
