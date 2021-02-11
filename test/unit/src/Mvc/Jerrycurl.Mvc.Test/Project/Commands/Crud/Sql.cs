using Jerrycurl.Mvc.Projections;
using Jerrycurl.Mvc.Test.Conventions.Models;

namespace Jerrycurl.Mvc.Test.Conventions.Queries.Crud
{
    public class Sql_cssql : ProcPage<string, object>
    {
        public Sql_cssql(IProjection model, IProjection result)
            : base(model, result)
        {

        }

        public override void Execute() => this.WriteLiteral(this.Model);
    }
}
