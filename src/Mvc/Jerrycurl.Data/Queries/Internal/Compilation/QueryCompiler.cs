using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Jerrycurl.Collections;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Data.Queries.Internal.Parsing;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Reflection;
using Jerrycurl.Data.Queries.Internal.IO;
using Jerrycurl.Data.Queries.Internal.Caching;
using System.Collections;
using System.Security.Cryptography;
using Jerrycurl.Data.Queries.Internal.IO.Writers;
using Jerrycurl.Data.Queries.Internal.IO.Readers;

namespace Jerrycurl.Data.Queries.Internal.Compilation
{
    internal class QueryCompiler
    {
        public const bool UseTryCatchExpressions = true;

        private delegate void ListInternalWriter(IDataReader dataReader, ElasticArray lists, ElasticArray aggregates, ElasticArray helpers, ISchema schema);
        private delegate void ListInternalInitializer(ElasticArray lists);
        private delegate object AggregateInternalReader(ElasticArray lists, ElasticArray aggregates, ISchema schema);
        private delegate TItem EnumerateInternalReader<TItem>(IDataReader dataReader, ElasticArray helpers, ISchema schema);

        private ListFactory CompileBuffer(ListResult result, Expression initialize, Expression writeOne, Expression writeAll)
        {
            ParameterExpression[] initArgs = new[] { Arguments.Lists };
            ParameterExpression[] writeArgs = new[] { Arguments.DataReader, Arguments.Lists, Arguments.Aggregates, Arguments.Helpers, Arguments.Schema };

            ListInternalInitializer initializeFunc = this.Compile<ListInternalInitializer>(initialize, initArgs);
            ListInternalWriter writeOneFunc = this.Compile<ListInternalWriter>(writeOne, writeArgs);
            ListInternalWriter writeAllFunc = this.Compile<ListInternalWriter>(writeAll, writeArgs);

            ElasticArray helpers = this.GetHelperBuffer(result.Helpers);
            ISchema schema = result.Schema;

            if (result.QueryType == QueryType.Aggregate)
            {
                AggregateAttribute[] header = result.Aggregates.Select(a => a.Attribute).NotNull().ToArray();

                return new ListFactory()
                {
                    Initialize = buf =>
                    {
                        buf.AggregateHeader.AddRange(header);

                        initializeFunc(buf.ListData);
                    },
                    WriteOne = (buf, dr) => writeOneFunc(dr, buf.ListData, buf.AggregateData, helpers, schema),
                    WriteAll = (buf, dr) =>
                    {
                        buf.AggregateHeader.AddRange(header);

                        writeAllFunc(dr, buf.ListData, buf.AggregateData, helpers, schema);
                    },
                };
            }
            else
            {
                return new ListFactory()
                {
                    WriteAll = (buf, dr) => writeAllFunc(dr, buf.ListData, buf.AggregateData, helpers, schema),
                    WriteOne = (buf, dr) => writeOneFunc(dr, buf.ListData, buf.AggregateData, helpers, schema),
                    Initialize = buf => initializeFunc(buf.ListData),
                };
            }
        }

        public ListFactory Compile(ListResult result)
        {
            List<ParameterExpression> variables = new List<ParameterExpression>();

            List<Expression> initList = new List<Expression>();
            List<Expression> oneList = new List<Expression>();
            List<Expression> allList = new List<Expression>();

            List<Expression> body = new List<Expression>();

            foreach (ListTarget index in result.Indices.DistinctBy(i => i.BufferIndex))
            {
                Expression prepareExpression = this.GetPrepareExpression(index);
                Expression writeExpresssion = this.GetWriterExpression(writer);

                initList.Add(writeExpresssion);
                oneList.Add(prepareExpression);
                allList.Add(writeExpresssion);
                allList.Add(prepareExpression);

                variables.Add(index.Variable);

                if (index.Join != null)
                    variables.Add(index.Join?.Buffer);
            }

            foreach (HelperWriter writer in result.Helpers)
            {
                Expression writeExpression = this.GetWriterExpression(writer);

                oneList.Add(writeExpression);
                allList.Add(writeExpression);

                variables.Add(writer.Variable);
            }

            foreach (AggregateWriter writer in result.Aggregates)
                body.Add(this.GetWriterExpression(writer));

            foreach (ListWriter writer in result.Lists)
                body.Add(this.GetWriterExpression(writer));

            oneList.AddRange(body);
            allList.Add(this.GetDataReaderLoopExpression(body));

            Expression initialize = this.GetBlockOrExpression(initList);
            Expression writeOne = this.GetBlockOrExpression(oneList, variables);
            Expression writeAll = this.GetBlockOrExpression(allList, variables);

            return this.CompileBuffer(result, initialize, writeOne, writeAll);
        }

        public AggregateFactory Compile(AggregateResult result)
        {
            List<Expression> body = new List<Expression>();

            if (result.Value != null)
                body.Add(this.GetBinderExpression(result.Value));

            if (result.List != null)
                body.Add(this.GetBinderExpression(result.List));

            ParameterExpression[] arguments = new[] { Arguments.Lists, Arguments.Aggregates, Arguments.Schema };
            AggregateInternalReader reader = this.Compile<AggregateInternalReader>(body, arguments);

            ISchema schema = result.Schema;

            return buf => reader(buf.ListData, buf.AggregateData, schema);
        }

        public EnumerateFactory<TItem> Compile<TItem>(EnumerateResult result)
        {
            List<Expression> body = new List<Expression>();

            if (result.Value == null)
                return _ => default;

            foreach (HelperWriter writer in result.Helpers)
                body.Add(this.GetWriterExpression(writer));

            body.Add(this.GetBinderExpression(result.Value));

            ParameterExpression[] arguments = new[] { Arguments.DataReader, Arguments.Helpers, Arguments.Schema };
            EnumerateInternalReader<TItem> reader = this.Compile<EnumerateInternalReader<TItem>>(body, arguments);

            ElasticArray helpers = this.GetHelperBuffer(result.Helpers);
            ISchema schema = result.Schema;

            return dr => reader(dr, helpers, schema);
        }

        private Expression GetBufferGetOrSetExpression(ListTarget index, Expression buffer, Expression defaultExpression)
        {
            Expression assignNew = Expression.Assign(buffer, Expression.Assign(index.Variable, defaultExpression));
            Expression assignOld = Expression.Assign(index.Variable, Expression.Convert(buffer, defaultExpression.Type));
            Expression indexIsNull = Expression.ReferenceEqual(buffer, Expression.Constant(null));

            return Expression.IfThenElse(indexIsNull, assignNew, assignOld);
        }

        #region " Prepare "
        private Expression GetPrepareExpression(ListTarget index)
        {
            Expression newList;

            if (index.Join == null)
                newList = this.GetBinderExpression(index.NewList);
            else
                newList = Expression.New(this.GetDictionaryType(index.Join.Key));

            Expression buffer = this.GetElasticIndexExpression(Arguments.Lists, index.BufferIndex);
            Expression assignNew = Expression.Assign(buffer, Expression.Assign(index.Variable, newList));
            Expression assignOld = Expression.Assign(index.Variable, Expression.Convert(buffer, newList.Type));
            Expression indexIsNull = Expression.ReferenceEqual(buffer, Expression.Constant(null));

            return Expression.IfThenElse(indexIsNull, assignNew, assignOld);
        }
        #endregion
        #region " Initialize "
        private Expression GetInitializeExpression(JoinWriter writer)
        {
            return null;
        }
        #endregion

        #region " Writers "
        private Expression GetWriterExpression(ListWriter writer)
        {
            Expression value = this.GetBinderExpression(writer.Value);
            Expression bufferSource, bufferTarget;

            if (writer.Index.Join == null)
            {
                bufferSource = writer.Index.Variable;
                bufferTarget = this.GetElasticIndexExpression(Arguments.Lists, writer.Index.BufferIndex);
            }
            else
                bufferSource = bufferTarget = this.GetElasticIndexExpression(writer.Index.Join.Buffer, writer.Index.Join.BufferIndex);

            if (writer.Index.NewList == null)
            {
                Expression convertValue = this.GetConvertOrExpression(value, typeof(object));

                return Expression.Assign(bufferTarget, convertValue);
            }
            else
            {
                Expression indexNotNull = this.GetIsNotNullExpression(bufferSource);

                Expression newList = this.GetBinderExpression(writer.Index.NewList);
                Expression assignNew = Expression.Assign(bufferTarget, newList);
                Expression getOrAdd = Expression.Condition(indexNotNull, bufferSource, assignNew);
                Expression listValue = this.GetConvertOrExpression(getOrAdd, newList.Type);

                return Expression.Call(listValue, writer.Metadata.Composition.Add, value);
            }
        }
        
        private Expression GetWriterExpression(HelperWriter writer)
        {
            Expression helperIndex = this.GetElasticIndexExpression(Arguments.Helpers, writer.BufferIndex);
            Expression castValue = Expression.Convert(helperIndex, writer.Variable.Type);

            return Expression.Assign(writer.Variable, castValue);
        }

        private Expression GetWriterExpression(AggregateWriter writer, bool useTryCatch = true)
        {
            Expression aggregateIndex = this.GetElasticIndexExpression(Arguments.Aggregates, writer.Attribute.AggregateIndex.Value);
            Expression aggregateValue = this.GetBinderExpression(writer.Value);

            Expression isDbNull = writer.Value.IsDbNull ?? this.GetIsDbNullExpression(writer.Value);
            Expression value = writer.Value.Variable;

            if (value == null)
            {
                value = this.GetValueExpression(writer.Value);
                value = this.GetConvertExpression(writer.Value, value);

                if (useTryCatch)
                    value = this.GetTryCatchExpression(writer.Value, value);
            }

            return Expression.Condition(isDbNull, Expression.Constant(null), this.GetConvertOrExpression(value, typeof(object)));
        }

        #endregion

        #region " Keys "
        private Expression GetKeyInitValueExpression(DataReader reader, bool useTryCatch = true)
        {
            Expression value = this.GetValueExpression(reader);
            Expression convertedValue = this.GetConvertExpression(reader, value);

            if (useTryCatch)
                convertedValue = this.GetTryCatchExpression(reader, convertedValue);

            if (reader.CanBeDbNull)
            {
                Expression isDbNull = this.GetIsDbNullExpression(reader);
                Expression assignNull = Expression.Assign(reader.IsDbNull, isDbNull);

                convertedValue = Expression.Condition(assignNull, Expression.Default(convertedValue.Type), convertedValue);
            }

            return Expression.Assign(reader.Variable, convertedValue);
        }

        private Expression GetKeyInitArrayExpression(ListTarget index)
        {
            Expression newKey = this.GetNewCompositeKeyExpression(index.Join.Key);
            Expression assignKey = Expression.Assign(index.Join.Key.Variable, newKey);
            Expression tryGet = this.GetDictionaryTryGetExpression(index.Variable, assignKey, index.Join.Buffer);
            Expression newBuffer = Expression.New(typeof(ElasticArray));
            Expression bufferIndex = this.GetElasticIndexExpression(index.Join.Buffer, index.Join.BufferIndex);
            Expression assignBuffer = Expression.Assign(index.Join.Buffer, newBuffer);
            Expression addArray = this.GetDictionaryAddExpression(index.Variable, index.Join.Key.Variable, assignBuffer);
            Expression getOrAdd = Expression.IfThenElse(tryGet, Expression.Default(typeof(void)), addArray);

            IEnumerable<ParameterExpression> isNullVars = index.Join.Key.Values.Where(v => v.CanBeDbNull).Select(v => v.IsDbNull).ToList();

            if (isNullVars.Any())
            {
                Expression isNull = this.GetAndConditionExpression(isNullVars);
                Expression setNull = Expression.Assign(index.Join.Buffer, Expression.Constant(null));

                getOrAdd = Expression.IfThenElse(isNull, setNull, getOrAdd);
            }

            return getOrAdd;
        }

        private Expression GetKeyBlockExpression(IEnumerable<KeyReader> primaryKeys, IEnumerable<KeyReader> joinKeys, Expression body)
        {
            List<Expression> expressions = new List<Expression>();
            List<ParameterExpression> variables = new List<ParameterExpression>();

            foreach (DataReader reader in joinKeys.SelectMany(k => k.Values).Distinct())
            {
                expressions.Add(this.GetKeyInitValueExpression(reader));

                if (reader.CanBeDbNull)
                    variables.Add(reader.IsDbNull);

                variables.Add(reader.Variable);
            }

            foreach (ListTarget index in indices)
            {
                expressions.Add(this.GetKeyInitArrayExpression(index));

                variables.Add(index.Join.Buffer);
            }

            expressions.Add(body);

            Expression block = this.GetBlockOrExpression(expressions, variables);


            if (primaryKeys.Any())
            {
                IEnumerable<DataReader> values = primaryKeys.SelectMany(k => k.Values).Distinct();

                Expression isNull = this.GetOrConditionExpression(values, this.GetIsDbNullExpression);

                return Expression.IfThenElse(isNull, Expression.Default(typeof(void)), block);
            }

            return block;
        }

        private Expression GetKeyBlockExpression(KeyReader primaryKey, IEnumerable<ListTarget> indices, Expression body)
        {
            List<Expression> expressions = new List<Expression>();
            List<ParameterExpression> variables = new List<ParameterExpression>();

            foreach (DataReader reader in indices.SelectMany(k => k.Join.Key.Values).Distinct())
            {
                expressions.Add(this.GetKeyInitValueExpression(reader));

                if (reader.CanBeDbNull)
                    variables.Add(reader.IsDbNull);

                variables.Add(reader.Variable);
            }

            foreach (ListTarget index in indices)
            {
                expressions.Add(this.GetKeyInitArrayExpression(index));

                variables.Add(index.Join.Buffer);
            }

            expressions.Add(body);

            Expression block = this.GetBlockOrExpression(expressions, variables);

            if (primaryKey != null)
            {
                Expression missingKey = this.GetOrConditionExpression(primaryKey.Values, this.GetIsDbNullExpression);

                return Expression.Condition(missingKey, Expression.Default(block.Type), block);
            }

            return block;
        }
        #endregion

        #region " Binders "

        private Expression GetBinderExpression(BaseReader reader) => reader switch
        {
            ColumnReader r => this.GetBinderExpression(r, r.IsDbNull, r.Variable, r.CanBeDbNull),
            AggregateReader r => this.GetBinderExpression(r, r.IsDbNull, r.Variable, r.CanBeDbNull),
            NewReader r => this.GetBinderExpression(r),
            ListReader r => this.GetBinderExpression(r),
            _ => throw new InvalidOperationException(),
        };

        private Expression GetBinderExpression(ListReader reader)
        {
            Expression arrayIsNull = Expression.ReferenceNotEqual(reader.Index.Join.Buffer, Expression.Constant(null));
            Expression arrayIndex = this.GetElasticIndexExpression(reader.Index.Join.Buffer, reader.Index.Join.BufferIndex);

            if (reader.List == null)
            {
                Expression ifArray = Expression.Condition(arrayIsNull, arrayIndex, Expression.Constant(null));

                return Expression.Convert(ifArray, reader.Metadata.Type);
            }
            else
            {
                Expression newList = this.GetBinderExpression(reader.List);
                Expression assignList = Expression.Assign(arrayIndex, newList);
                Expression indexIsNull = Expression.ReferenceEqual(arrayIndex, Expression.Constant(null));
                Expression getOrAdd = Expression.Condition(arrayIsNull, assignList, arrayIndex);
                Expression ifArray = Expression.Condition(arrayIsNull, getOrAdd, Expression.Constant(null));

                return Expression.Convert(ifArray, reader.Metadata.Type);
            }
        }

        private Expression GetBinderExpression(DataReader reader, Expression isDbNull, Expression value, bool canBeDbNull, bool useTryCatch = true)
        {
            isDbNull ??= this.GetIsDbNullExpression(reader);

            if (value == null)
            {
                value = this.GetValueExpression(reader);
                value = this.GetConvertExpression(reader, value);

                if (useTryCatch)
                    value = this.GetTryCatchExpression(reader, value);
            }

            if (canBeDbNull)
                value = Expression.Condition(isDbNull, Expression.Default(reader.Metadata.Type), value);

            return value;
        }

        private Expression GetBinderExpression(NewReader reader)
        {
            NewExpression newExpression = reader.Metadata.Composition.Construct;
            Expression memberInit = Expression.MemberInit(newExpression, reader.Properties.Select(r =>
            {
                if (!r.Metadata.HasFlag(BindingMetadataFlags.Writable))
                    throw BindingException.IsReadOnly(r.Metadata);

                Expression value = this.GetBinderExpression(r);

                return Expression.Bind(r.Metadata.Member, value);
            }));

            return this.GetKeyBlockExpression(reader.PrimaryKey, reader.Joins, memberInit);
        }

        private Expression GetBinderExpression(DynamicReader reader)
        {
            ParameterExpression variable = Expression.Variable(reader.Metadata.Composition.Construct.Type);
            NewExpression newExpression = reader.Metadata.Composition.Construct;

            List<Expression> body = new List<Expression>()
            {
                Expression.Assign(variable, newExpression),
            };

            foreach (BaseReader propertyReader in reader.Properties)
            {
                string propertyName = propertyReader.Identity.Schema.Notation.Member(propertyReader.Identity.Name);

                Expression propertyValue = this.GetBinderExpression(propertyReader);
                Expression objectValue = propertyValue.Type.IsValueType ? Expression.Convert(propertyValue, typeof(object)) : propertyValue;
                Expression addDynamic = Expression.Call(variable, reader.Metadata.Composition.AddDynamic, Expression.Constant(propertyName), objectValue);

                body.Add(addDynamic);
            }

            body.Add(variable);

            return Expression.Block(new[] { variable }, body);
        }

        #endregion

        #region  " IsDbNull "

        private Expression GetIsDbNullExpression(DataReader reader) => reader switch
        {
            ColumnReader r => this.GetIsDbNullExpression(r),
            AggregateReader r => this.GetIsDbNullExpression(r),
            _ => throw new InvalidOperationException(),
        };

        private Expression GetIsDbNullExpression(ColumnReader reader)
        {
            MethodInfo isNullMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), new[] { typeof(int) });

            return Expression.Call(Arguments.DataReader, isNullMethod, Expression.Constant(reader.Column.Index));
        }

        private Expression GetIsDbNullExpression(AggregateReader reader)
        {
            Expression bufferIndex;

            if (reader.Attribute.AggregateIndex != null)
                bufferIndex = this.GetElasticIndexExpression(Arguments.Aggregates, reader.Attribute.AggregateIndex.Value);
            else if (reader.Attribute.ListIndex != null)
                bufferIndex = this.GetElasticIndexExpression(Arguments.Lists, reader.Attribute.ListIndex.Value);
            else
                throw new InvalidOperationException();

            return Expression.ReferenceEqual(bufferIndex, Expression.Constant(null));
        }

        #endregion

        #region " Values "
        private Expression GetValueExpression(BaseReader reader) => reader switch
        {
            ColumnReader r => this.GetValueExpression(r),
            AggregateReader r => this.GetValueExpression(r),
            _ => throw new InvalidOperationException(),
        };

        private Expression GetValueExpression(AggregateReader reader)
        {
            if (reader.Attribute.AggregateIndex != null)
                return this.GetElasticIndexExpression(Arguments.Aggregates, reader.Attribute.AggregateIndex.Value);
            else if (reader.Attribute.ListIndex != null)
                return this.GetElasticIndexExpression(Arguments.Lists, reader.Attribute.ListIndex.Value);
            else
                throw new InvalidOperationException();
        }   

        private Expression GetValueExpression(ColumnReader reader)
        {
            MethodInfo readMethod = this.GetValueReaderMethod(reader);

            Expression index = Expression.Constant(reader.Column.Index);
            Expression dataReader = Arguments.DataReader;

            if (readMethod.DeclaringType != typeof(IDataReader) && readMethod.DeclaringType != typeof(IDataRecord))
                dataReader = Expression.Convert(dataReader, readMethod.DeclaringType);

            return Expression.Call(dataReader, readMethod, index);
        }

        private MethodInfo GetValueReaderMethod(ColumnReader reader)
        {
            BindingColumnInfo bindingInfo = new BindingColumnInfo()
            {
                Metadata = reader.Metadata,
                Column = reader.Column,
            };

            MethodInfo readMethod = reader.Metadata.Value?.Read?.Invoke(bindingInfo);

            if (readMethod == null)
                readMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), new Type[] { typeof(int) });

            return readMethod;
        }

        #endregion

        #region " Convert "
        private Expression GetConvertExpression(BaseReader reader, Expression value) => reader switch
        {
            AggregateReader r => this.GetConvertExpression(r, value),
            ColumnReader r => this.GetConvertExpression(r, value),
            _ => throw new InvalidOperationException(),
        };

        private Expression GetConvertExpression(AggregateReader reader, Expression value)
            => this.GetConvertOrExpression(value, reader.Metadata.Type);

        private Expression GetConvertExpression(ColumnReader reader, Expression value)
        {
            Type targetType = reader.KeyType ?? reader.Metadata.Type;
            ParameterExpression variable = Expression.Variable(value.Type);

            BindingValueInfo valueInfo = new BindingValueInfo()
            {
                SourceType = value.Type,
                TargetType = targetType,
                CanBeNull = false,
                CanBeDbNull = false,
                Metadata = reader.Metadata,
                Value = variable,
                Helper = reader.Helper,
            };

            Expression convertedValue;

            try
            {
                convertedValue = reader.Metadata.Value?.Convert?.Invoke(valueInfo);
            }
            catch (Exception ex)
            {
                throw BindingException.InvalidCast(reader.Metadata, ex);
            }

            if (convertedValue == null || object.ReferenceEquals(convertedValue, variable))
                return value;
            else if (convertedValue is UnaryExpression ue)
            {
                if (ue.NodeType == ExpressionType.Convert && ue.Operand.Equals(variable))
                    return Expression.Convert(value, ue.Type);
                else if (ue.NodeType == ExpressionType.ConvertChecked && ue.Operand.Equals(variable))
                    return Expression.ConvertChecked(value, ue.Type);
            }

            Expression assignValue = Expression.Assign(variable, value);

            return Expression.Block(new[] { variable }, assignValue, convertedValue);
        }

        #endregion

        #region " Helpers "

        private Expression GetConvertOrExpression(Expression expression, Type type)
        {
            if (!expression.Type.IsValueType && type.IsValueType)
                return Expression.Convert(expression, type);

            return expression;
        }

        private Expression GetBlockOrExpression(IList<Expression> expressions, IList<ParameterExpression> variables = null)
        {
            if (expressions.Count == 1 && (variables == null || !variables.Any()))
                return expressions[0];
            else if (variables == null)
                return Expression.Block(expressions);
            else
                return Expression.Block(variables.NotNull(), expressions);
        }

        private TDelegate Compile<TDelegate>(IList<Expression> body, IList<ParameterExpression> arguments)
        {
            Expression block = this.GetBlockOrExpression(body);

            return Expression.Lambda<TDelegate>(block, arguments).Compile();
        }

        private TDelegate Compile<TDelegate>(Expression block, params ParameterExpression[] arguments)
            => this.Compile<TDelegate>(new[] { block }, arguments);

        private Expression GetDataReaderLoopExpression(IList<Expression> body)
        {
            LabelTarget label = Expression.Label();

            Expression callRead = Expression.Call(Arguments.DataReader, typeof(IDataReader).GetMethod(nameof(IDataReader.Read)));
            Expression ifRead = Expression.IfThenElse(callRead, this.GetBlockOrExpression(body), Expression.Break(label));

            return Expression.Loop(ifRead, label);
        }

        private Expression GetDictionaryAddExpression(Expression dictionary, Expression key, Expression value)
        {
            MethodInfo addMethod = dictionary.Type.GetMethod("Add");

            return Expression.Call(dictionary, addMethod, key, value);
        }

        private Expression GetDictionaryTryGetExpression(Expression dictionary, Expression key, Expression outVariable)
        {
            MethodInfo tryGetMethod = dictionary.Type.GetMethod("TryGetValue");

            return Expression.Call(dictionary, tryGetMethod, key, outVariable);
        }

        private Expression GetNewCompositeKeyExpression(KeyReader key)
        {
            if (key.Values.Count == 1)
                return key.Values[0].Variable;

            ConstructorInfo ctor = key.Variable.Type.GetConstructors()[0];

            return Expression.New(ctor, key.Values.Select(v => v.Variable));
        }

        private ElasticArray GetHelperBuffer(IEnumerable<HelperWriter> writers)
        {
            ElasticArray array = new ElasticArray();

            foreach (HelperWriter writer in writers)
                array[writer.BufferIndex] = writer.Object;

            return array;
        }

        private Expression GetIsNotNullExpression(Expression value)
            => Expression.ReferenceNotEqual(value, Expression.Constant(null));

        private Expression GetTryCatchExpression(BaseReader reader, Expression expression)
        {
            if (this.IsRunningNetFramework() && expression.Type.IsValueType)
                return expression;

            ParameterExpression ex = Expression.Variable(typeof(Exception));

            MethodInfo constructor = typeof(QueryCompiler).GetStaticMethod(nameof(QueryCompiler.GetInvalidCastException), typeof(ISchema), typeof(string), typeof(Exception));

            Expression newException = Expression.Call(constructor, Arguments.Schema, Expression.Constant(reader.Identity.Name), ex);
            CatchBlock catchBlock = Expression.Catch(ex, Expression.Throw(newException, expression.Type));

            return Expression.TryCatch(expression, catchBlock);
        }

        private Type GetDictionaryType(Type keyType)
            => typeof(Dictionary<,>).MakeGenericType(keyType, typeof(ElasticArray));

        private Expression GetElasticIndexExpression(Expression arrayExpression, int index)
        {
            PropertyInfo indexer = arrayExpression.Type.GetProperty("Item");

            return Expression.Property(arrayExpression, indexer, Expression.Constant(index));
        }

        private Expression GetOrConditionExpression<T>(IEnumerable<T> values, Func<T, Expression> condition, Expression emptyValue = null)
            => this.GetConditionExpression(values, condition, Expression.OrElse, emptyValue);

        private Expression GetAndConditionExpression<T>(IEnumerable<T> values, Func<T, Expression> condition, Expression emptyValue = null)
            => this.GetConditionExpression(values, condition, Expression.AndAlso, emptyValue);

        private Expression GetOrConditionExpression(IEnumerable<Expression> conditions, Expression emptyValue = null)
            => this.GetConditionExpression(conditions, Expression.OrElse, emptyValue);

        private Expression GetAndConditionExpression(IEnumerable<Expression> conditions, Expression emptyValue = null)
            => this.GetConditionExpression(conditions, Expression.AndAlso, emptyValue);

        private Expression GetConditionExpression<T>(IEnumerable<T> values, Func<T, Expression> condition, Func<Expression, Expression, Expression> gateFactory, Expression emptyValue = null)
            => this.GetConditionExpression(values.Select(condition), gateFactory, emptyValue);

        private Expression GetConditionExpression(IEnumerable<Expression> conditions, Func<Expression, Expression, Expression> gateFactory, Expression emptyValue = null)
        {
            if (conditions == null || !conditions.Any())
                return emptyValue;

            Expression expr = conditions.First();

            foreach (Expression condition in conditions.Skip(1))
                expr = gateFactory(expr, condition);

            return expr;
        }

        private bool IsAssignableFrom(Type left, Type right) => left.IsAssignableFrom(right);

        private bool IsRunningNetFramework() => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");

        private static BindingException GetInvalidCastException(ISchema schema, string attributeName, Exception innerException)
        {
            IBindingMetadata metadata = schema.Require<IBindingMetadata>(attributeName);

            return BindingException.InvalidCast(metadata, innerException);
        }

        #endregion

        private static class Arguments
        {
            public static ParameterExpression DataReader { get; } = Expression.Parameter(typeof(IDataReader), "dataReader");
            public static ParameterExpression Lists { get; } = Expression.Parameter(typeof(ElasticArray), "lists");
            public static ParameterExpression Aggregates { get; } = Expression.Parameter(typeof(ElasticArray), "aggregates");
            public static ParameterExpression Helpers { get; } = Expression.Parameter(typeof(ElasticArray), "helpers");
            public static ParameterExpression Schema { get; } = Expression.Parameter(typeof(ISchema), "schema");
        }
    }
}
