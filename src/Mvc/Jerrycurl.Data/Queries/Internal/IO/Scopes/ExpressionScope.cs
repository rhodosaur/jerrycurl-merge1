using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    public class ExpressionScope : Scope
    {
        public Expression Expression { get; set; }

        public ExpressionScope(Expression expression)
        {
            this.Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }
        
    }
}
