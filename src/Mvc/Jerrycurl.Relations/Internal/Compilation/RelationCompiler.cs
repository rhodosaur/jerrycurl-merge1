using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Jerrycurl.Collections;
using Jerrycurl.Relations.Internal.Queues;
using Jerrycurl.Relations.Internal.IO;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Relations.Internal;

namespace Jerrycurl.Relations.Internal.Compilation
{
    internal class RelationCompiler
    {
        private delegate void BufferInternalWriter(IField2[] fields, IRelationQueue[] queues, IField2 source, IField2 model, DotNotation2 notation, Delegate[] binders, IRelationMetadata[] metadata);

        public BufferWriter Compile(BufferTree tree)
        {
            DotNotation2 notation = tree.Notation;
            Delegate[] binders = this.GetBindersArgument(tree);
            IRelationMetadata[] metadata = this.GetMetadataArgument(tree);

            BufferInternalWriter initializer = this.Compile(tree.Source, tree.Queues, tree.Fields.Count);
            List<BufferInternalWriter> writers = tree.Queues.Select(this.Compile).ToList();

            return new BufferWriter()
            {
                Initializer = this.Recompile(initializer, notation, binders, metadata),
                Queues = writers.Select(w => this.Recompile(w, notation, binders, metadata)).ToArray(),
            };
        }

        private Action<RelationBuffer> Recompile(BufferInternalWriter writer, DotNotation2 notation, Delegate[] binders, IRelationMetadata[] metadata)
            => buf => writer(buf.Fields, buf.Queues, buf.Source, buf.Model, notation, binders, metadata);

        private BufferInternalWriter Compile(SourceReader reader, IList<QueueReader> queueReaders, int metadataOffset)
        {
            Expression body = this.GetInitializerExpression(reader, queueReaders, metadataOffset);

            return this.Compile(body);
        }

        private BufferInternalWriter Compile(QueueReader reader)
        {
            Expression body = this.GetReadWriteExpression(reader);

            return this.Compile(body);
        }

        private BufferInternalWriter Compile(Expression body)
        {
            ParameterExpression[] innerArgs = new[] { Arguments.Fields, Arguments.Queues, Arguments.Source, Arguments.Model, Arguments.Notation, Arguments.Binders, Arguments.Metadata };

            return Expression.Lambda<BufferInternalWriter>(body, innerArgs).Compile();
        }

        #region " Initializer "

        public Expression GetInitializerExpression(SourceReader reader, IList<QueueReader> queueReaders, int metadataOffset)
        {
            List<Expression> expressions = new List<Expression>();

            foreach (QueueReader queue in queueReaders)
                expressions.Add(this.GetAssignNewQueueExpression(queue.Index, metadataOffset));

            expressions.Add(this.GetReadWriteExpression(reader));

            return this.GetBlockOrExpression(expressions);
        }



        #endregion

        #region " I/O "
        public Expression GetReadWriteExpression(SourceReader reader)
            => this.GetReadWriteExpression(reader, null);

        public Expression GetReadWriteExpression(QueueReader reader)
        {
            Expression queueIndex = this.GetQueueIndexExpression(reader.Index);
            Expression assignVariable = Expression.Assign(reader.Index.Variable, queueIndex);
            Expression parentValue = this.GetQueuePropertyExpression(reader.Index, "List");
            Expression readWrite = this.GetReadWriteExpression(reader, parentValue);

            return this.GetBlockOrExpression(new[] { assignVariable, readWrite }, new[] { reader.Index.Variable });
        }

        public Expression GetReadWriteExpression(NodeReader reader, Expression parentValue)
        {
            ParameterExpression variable = this.GetVariable(reader.Metadata);

            Expression value = this.GetReadExpression(reader, parentValue);
            Expression assignVariable = Expression.Assign(variable, value);

            List<Expression> body = new List<Expression>();
            List<Expression> notBody = new List<Expression>();
            List<Expression> nullBody = new List<Expression>();

            body.Add(assignVariable);
            body.AddRange(this.GetWriteExpressions(reader, parentValue, variable));
            notBody.AddRange(this.GetReadWriteExpressions(reader, variable));
            nullBody.AddRange(this.GetWriteMissingExpressions(reader));

            if (this.IsNullableType(value.Type) && (notBody.Any() || nullBody.Any()))
            {
                Expression isNull = Expression.ReferenceEqual(variable, Expression.Constant(null));
                Expression notBlock = this.GetBlockOrExpression(notBody);
                Expression nullBlock = this.GetBlockOrExpression(nullBody);
                Expression ifThen = Expression.IfThenElse(isNull, nullBlock, notBlock);

                body.Add(ifThen);
            }
            else
                body.AddRange(notBody);

            return this.GetBlockOrExpression(body, new[] { variable });
        }

        private IEnumerable<Expression> GetReadWriteExpressions(NodeReader reader, Expression value)
        {
            foreach (PropertyReader propReader in reader.Properties)
                yield return this.GetReadWriteExpression(propReader, value);
        }
        #endregion

        #region " Queues "
        private Type GetQueueGenericType(QueueIndex queue) => typeof(RelationQueue<,>).MakeGenericType(queue.List.Type, queue.Item.Type);
        private Type GetQueueItemGenericType(QueueIndex queue) => typeof(RelationQueueItem<>).MakeGenericType(queue.List.Type);

        private Expression GetAssignNewQueueExpression(QueueIndex index, int metadataOffset)
        {
            Expression arrayIndex = Expression.ArrayAccess(Arguments.Queues, Expression.Constant(index.Buffer));
            Expression newQueue = this.GetNewQueueExpression(index, metadataOffset);

            return Expression.Assign(arrayIndex, newQueue);
        }

        private Expression GetNewQueueExpression(QueueIndex index, int metadataOffset)
        {
            Type type = this.GetQueueGenericType(index);
            ConstructorInfo ctor = type.GetConstructors()[0];

            Expression metadata = Expression.ArrayAccess(Arguments.Metadata, Expression.Constant(metadataOffset + index.Buffer));
            Expression queueType = Expression.Constant(index.Type);

            return Expression.New(ctor, metadata, queueType);
        }

        private Expression GetQueueAddExpression(QueueWriter writer, Expression value)
        {
            Type queueType = this.GetQueueGenericType(writer.Next);
            MethodInfo addMethod = queueType.GetMethod("Enqueue");

            Expression queue = this.GetQueueIndexExpression(writer.Next);
            Expression queueItem = this.GetNewQueueItemExpression(writer, value);

            return Expression.Call(queue, addMethod, queueItem);
        }

        private Expression GetQueueIndexExpression(QueueIndex index)
        {
            Expression arrayIndex = Expression.ArrayAccess(Arguments.Queues, Expression.Constant(index.Buffer));

            return Expression.Convert(arrayIndex, index.Variable.Type);
        }

        private Expression GetNewQueueItemExpression(QueueWriter writer, Expression value)
        {
            Type itemType = this.GetQueueItemGenericType(writer.Next);
            ConstructorInfo ctor = itemType.GetConstructors()[0];

            Expression namePart = this.GetFieldNameExpression(writer);

            return Expression.New(ctor, value, namePart, Arguments.Notation);
        }
        private Expression GetQueuePropertyExpression(QueueIndex queue, string propertyName)
            => Expression.Property(queue.Variable, propertyName);
        #endregion

        #region " Writers "
        private Expression GetWriteExpression(NodeWriter writer, Expression parentValue, Expression value) => writer switch
        {
            FieldWriter writer2 => this.GetWriteExpression(writer2, parentValue, value),
            QueueWriter writer2 => this.GetWriteExpression(writer2, value),
            _ => throw new InvalidOperationException(),
        };

        private Expression GetWriteExpression(FieldWriter writer, Expression parentValue, Expression value)
        {
            Expression bufferIndex = Expression.ArrayAccess(Arguments.Fields, Expression.Constant(writer.BufferIndex));
            Expression newField = this.GetNewFieldExpression(writer, parentValue, value);

            return Expression.Assign(bufferIndex, newField);
        }

        private Expression GetWriteExpression(QueueWriter writer, Expression value) => this.GetQueueAddExpression(writer, value);

        private IEnumerable<Expression> GetWriteExpressions(NodeReader reader, Expression parentValue, Expression value)
        {
            foreach (NodeWriter writer in reader.Writers)
            {
                Expression expression = this.GetWriteExpression(writer, parentValue, value);

                if (expression != null)
                    yield return expression;
            }
        }

        #endregion

        #region " Missing writers "
        private Expression GetWriteMissingExpression(FieldWriter writer)
        {
            Expression bufferIndex = Expression.ArrayAccess(Arguments.Fields, Expression.Constant(writer.BufferIndex));
            Expression newField = this.GetNewMissingExpression(writer);

            return Expression.Assign(bufferIndex, newField);
        }


        private Expression GetWriteMissingExpression(QueueWriter writer)
            => this.GetWriteExpression(writer, Expression.Constant(null, writer.Next.List.Type));

        private Expression GetWriteMissingExpression(NodeWriter writer) => writer switch
        {
            FieldWriter writer2 => this.GetWriteMissingExpression(writer2),
            QueueWriter writer2 => this.GetWriteMissingExpression(writer2),
            _ => throw new InvalidOperationException(),
        };

        private IEnumerable<Expression> GetWriteMissingExpressions(NodeReader reader)
        {
            foreach (PropertyReader propReader in reader.Properties)
            {
                foreach (NodeWriter writer in propReader.Writers)
                    yield return this.GetWriteMissingExpression(writer);
            }

            foreach (PropertyReader propReader in reader.Properties)
                foreach (Expression expression in this.GetWriteMissingExpressions(propReader))
                    yield return expression;
        }
        #endregion

        #region " Readers "

        private Expression GetReadExpression(NodeReader reader, Expression parentValue) => reader switch
        {
            QueueReader reader2 => this.GetReadExpression(reader2),
            SourceReader reader2 => this.GetReadExpression(reader2),
            PropertyReader reader2 => this.GetReadExpression(reader2, parentValue),
            _ => throw new InvalidOperationException(),
        };

        private Expression GetReadExpression(PropertyReader reader, Expression parentValue)
            => Expression.MakeMemberAccess(parentValue, reader.Metadata.Member);

        private Expression GetReadExpression(QueueReader reader)
            => this.GetQueuePropertyExpression(reader.Index, "Current");

        private Expression GetReadExpression(SourceReader reader)
        {
            Expression data = Expression.Property(Arguments.Source, nameof(IField2.Data));
            Expression value = Expression.Property(data, nameof(IFieldData.Value));

            return Expression.Convert(value, reader.Metadata.Type);
        }

        #endregion

        #region " Inputs "
        private Expression GetSourceNameExpression()
            => Expression.Property(Expression.Property(Arguments.Source, nameof(IField2.Identity)), nameof(FieldIdentity.Name));

        private Expression GetMetadataExpression(FieldWriter writer)
            => Expression.ArrayAccess(Arguments.Metadata, Expression.Constant(writer.BufferIndex));

        private Expression GetBinderExpression(FieldWriter writer)
        {
            Expression binderObject = Expression.ArrayAccess(Arguments.Binders, Expression.Constant(writer.BufferIndex));

            return binderObject;
        }

        #endregion

        #region " Fields "
        private Expression GetFieldNameExpression(NodeWriter writer)
        {
            if (writer.Queue != null)
            {
                MethodInfo nameMethod = writer.Queue.Variable.Type.GetMethod("GetFieldName");

                return Expression.Call(writer.Queue.Variable, nameMethod, Expression.Constant(writer.NamePart));
            }
            else
            {
                MethodInfo combineMethod = typeof(DotNotation2).GetMethod(nameof(DotNotation2.Combine), new[] { typeof(string), typeof(string) });

                Expression sourceName = this.GetSourceNameExpression();

                return Expression.Call(Arguments.Notation, combineMethod, sourceName, Expression.Constant(writer.NamePart));
            }
        }

        private Expression GetNewFieldExpression(FieldWriter writer, Expression parentValue, Expression value)
        {
            if (parentValue == null)
                return Arguments.Source;

            Type fieldType = typeof(Field2<,>).MakeGenericType(value.Type, parentValue.Type);
            Type dataType = typeof(FieldData<,>).MakeGenericType(value.Type, parentValue.Type);

            ConstructorInfo newFieldInfo = fieldType.GetConstructors()[0];
            ConstructorInfo newDataInfo = dataType.GetConstructors()[0];

            Expression relation = writer.Queue != null ? this.GetQueuePropertyExpression(writer.Queue, "List") : Expression.Constant(null);
            Expression index = writer.Queue != null ? this.GetQueuePropertyExpression(writer.Queue, "Index") : Expression.Constant(0);
            Expression binder = this.GetBinderExpression(writer);
            Expression isReadOnly = this.GetFieldIsReadOnlyExpression(writer);
            
            Expression name = this.GetFieldNameExpression(writer);
            Expression metadata = this.GetMetadataExpression(writer);
            Expression data = Expression.New(newDataInfo, relation, index, parentValue, value, binder);

            return Expression.New(newFieldInfo, name, metadata, data, Arguments.Model, isReadOnly);
        }

        private Expression GetNewMissingExpression(FieldWriter writer)
        {
            Type fieldType = typeof(Missing2<>).MakeGenericType(writer.Metadata.Type);
            ConstructorInfo ctor = fieldType.GetConstructors()[0];

            Expression name = this.GetFieldNameExpression(writer);
            Expression metadata = this.GetMetadataExpression(writer);

            return Expression.New(ctor, name, metadata, Arguments.Model);
        }

        private Expression GetFieldIsReadOnlyExpression(FieldWriter writer)
        {
            if (writer.Metadata.HasFlag(RelationMetadataFlags.Item) && writer.Metadata.WriteIndex == null)
                return Expression.Constant(true);
            else if (!writer.Metadata.HasFlag(RelationMetadataFlags.Writable))
                return Expression.Constant(true);

            return Expression.Constant(false);
        }

        #endregion

        #region " Helpers "
        private bool IsNullableType(Type type) => (!type.IsValueType || Nullable.GetUnderlyingType(type) != null);

        private ParameterExpression GetVariable(IRelationMetadata metadata, Type variableType = null)
        {
            string varName = "_" + metadata.Identity.Name.ToLower().Replace('.', '_');

            return Expression.Variable(variableType ?? metadata.Type, varName);
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
        #endregion

        #region " Arguments "

        private IRelationMetadata[] GetMetadataArgument(BufferTree tree)
        {
            IRelationMetadata[] metadata = new IRelationMetadata[tree.Fields.Count + tree.Queues.Count];

            foreach (FieldWriter writer in tree.Fields)
                metadata[writer.BufferIndex] = writer.Metadata;

            foreach (QueueReader queue in tree.Queues)
                metadata[tree.Fields.Count + queue.Index.Buffer] = queue.Metadata;

            return metadata;
        }

        private Delegate[] GetBindersArgument(BufferTree tree)
        {
            Delegate[] binders = new Delegate[tree.Fields.Count];

            foreach (FieldWriter writer in tree.Fields)
                binders[writer.BufferIndex] = this.GetBinderArgument(writer);

            return binders;
        }

        private Delegate GetBinderArgument(FieldWriter writer)
        {
            if (writer.Metadata.Parent == null)
                return null;

            Type binderType = typeof(FieldBinder<,>).MakeGenericType(writer.Metadata.Parent.Type, writer.Metadata.Type);

            ParameterExpression parentValue = writer.Metadata.Parent != null ? Expression.Parameter(writer.Metadata.Parent.Type) : null;
            ParameterExpression index = Expression.Parameter(typeof(int));
            ParameterExpression value = Expression.Parameter(writer.Metadata.Type);

            Expression bindExpression;

            if (writer.Metadata.HasFlag(RelationMetadataFlags.Item))
            {
                if (writer.Metadata.WriteIndex != null)
                {
                    Expression listValue = parentValue;

                    if (parentValue.Type != writer.Metadata.WriteIndex.DeclaringType)
                        listValue = Expression.Convert(listValue, writer.Metadata.WriteIndex.DeclaringType);

                    bindExpression = Expression.Call(listValue, writer.Metadata.WriteIndex, index, value);
                }
                else
                    bindExpression = Expression.Throw(Expression.New(typeof(NotIndexableException)));
            }
            else if (!writer.Metadata.HasFlag(RelationMetadataFlags.Writable))
                bindExpression = Expression.Throw(Expression.New(typeof(NotWritableException)));
            else if (writer.Metadata.Member != null)
                bindExpression = Expression.Assign(Expression.MakeMemberAccess(parentValue, writer.Metadata.Member), value);
            else
                return null;

            return Expression.Lambda(binderType, bindExpression, new[] { parentValue, index, value }).Compile();
        }
        #endregion

        private static class Arguments
        {
            public static ParameterExpression Fields { get; } = Expression.Parameter(typeof(IField2[]), "fields");
            public static ParameterExpression Model { get; } = Expression.Parameter(typeof(IField2), "model");
            public static ParameterExpression Source { get; } = Expression.Parameter(typeof(IField2), "source");
            public static ParameterExpression Metadata { get; } = Expression.Parameter(typeof(IRelationMetadata[]), "metadata");
            public static ParameterExpression Notation { get; } = Expression.Parameter(typeof(DotNotation2), "notation");
            public static ParameterExpression Binders { get; } = Expression.Parameter(typeof(Delegate[]), "binders");
            public static ParameterExpression Queues { get; } = Expression.Parameter(typeof(IRelationQueue[]), "queues");

        }
    }
}
