using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Relations
{
    public static class RelationExtensions
    {
        public static ITuple2 Row(this IRelation2 relation) => relation.Body.FirstOrDefault();
        public static IField2 Scalar(this IRelation2 relation) => relation.Body.FirstOrDefault()?.FirstOrDefault();
        public static IEnumerable<IField2> Column(this IRelation2 relation) => relation.Body.Select(t => t.FirstOrDefault());
    }
}
