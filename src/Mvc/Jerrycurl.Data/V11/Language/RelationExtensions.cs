using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Jerrycurl.Data.Commands;
using Jerrycurl.Data.Queries;
using Jerrycurl.Data.Sessions;
using Jerrycurl.Data.V11;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Language;

namespace Jerrycurl.Data.V11.Language
{
    public static class RelationExtensions
    {
        public static Query ToQuery(this IRelation2 relation, string queryText)
        {
            return new Query()
            {
                QueryText = queryText,
                Parameters = relation.ToParameters()
            };
        }

        public static Command ToCommand(this IRelation2 relation, string commandText)
        {
            return new Command()
            {
                CommandText = commandText,
                Parameters = relation.ToParameters()
            };
        }

        public static Command ToCommand(this IRelation2 relation, Func<IList<IParameter>, string> textBuilder)
        {
            ParameterStore2 store = new ParameterStore2();

            Command command = new Command()
            {
                Parameters = store,
            };

            using var reader = relation.GetReader();

            while (reader.Read())
            {
                IList<IParameter> parameters = store.Add(reader);

                command.CommandText += textBuilder(parameters) + Environment.NewLine;
            }

            return command;
        }

        public static IDataReader As(this IRelation2 relation, IEnumerable<string> header)
            => relation.GetDataReader(header);

        public static IDataReader As(this IRelation2 relation, params string[] header)
            => relation.GetDataReader(header);

        public static IList<IParameter> ToParameters(this IField2 source, params string[] header)
            => source.Select(header).ToParameters();

        public static IList<IParameter> ToParameters(this IField2 source, IEnumerable<string> header)
            => source.Select(header).ToParameters();

        public static IList<IParameter> ToParameters(this ITuple2 tuple)
            => new ParameterStore2().Add(tuple);

        public static IList<IParameter> ToParameters(this IRelation2 relation)
            => new ParameterStore2().Add(relation);

        public static IParameter ToParameter(this IField2 field, string parameterName)
            => new Parameter(parameterName, field);
    }
}
