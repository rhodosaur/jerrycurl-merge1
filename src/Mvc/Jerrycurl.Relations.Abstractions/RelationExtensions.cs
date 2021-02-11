using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Relations.V11;

namespace Jerrycurl.Relations
{
    public static class RelationExtensions
    {
        public static ITuple Row(this IRelation relation) => relation.FirstOrDefault();
        public static IField Scalar(this IRelation relation) => relation.FirstOrDefault()?.FirstOrDefault();
        public static IEnumerable<IField> Column(this IRelation relation) => relation.Select(t => t.FirstOrDefault());

        public static ITuple2 Row(this IRelation2 relation) => relation.Body.FirstOrDefault();
        public static IField2 Scalar(this IRelation2 relation) => relation.Body.FirstOrDefault()?.FirstOrDefault();
        public static IEnumerable<IField2> Column(this IRelation2 relation) => relation.Body.Select(t => t.FirstOrDefault());
    }
}
