using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Jerrycurl.Data.Queries.Internal.IO
{
    public class Scope
    {
        public List<ScopeVariable> Variables { get; set; }
        public List<Scope> Body { get; set; }

        public void Add(Expression expression)
        {
            if (expression != null)
                this.Body.Add(new ExpressionScope(expression));
        }

        public ParameterExpression Declare(string name, Type type = null)
        {
            this.Variables.Add(new ScopeVariable(name, null));
        }

        public ParameterExpression Var(string name)
        {
            return null;
        }



        public virtual Expression Build()
        {
            if (this.Body.Count == 1 && !this.Variables.Any())
                return this.Body[0].Build();
            else if (!this.Variables.Any())
                return Expression.Block(this.Body.Select(s => s.Build()));
            else
                return Expression.Block(this.Variables.Select(v => v.Build()), this.Body.Select(s => s.Build()));
        }
    }
}
