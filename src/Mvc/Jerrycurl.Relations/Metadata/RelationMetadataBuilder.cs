﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Jerrycurl.Collections;
using Jerrycurl.Diagnostics;
using Jerrycurl.Reflection;
using Jerrycurl.Relations.Metadata.Contracts;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Relations.Metadata
{
    public class RelationMetadataBuilder : Collection<IRelationContractResolver>, IMetadataBuilder<IRelationMetadata>
    {
        public IRelationContractResolver DefaultResolver { get; set; } = new DefaultRelationContractResolver();

        public IRelationMetadata GetMetadata(IMetadataBuilderContext context) => this.GetMetadata(context, context.Identity);

        public RelationMetadataBuilder()
        {
            
        }

        public RelationMetadataBuilder(IEnumerable<IRelationContractResolver> resolvers)
        {
            if (resolvers == null)
                throw new ArgumentNullException(nameof(resolvers));

            foreach (IRelationContractResolver resolver in resolvers)
                this.Add(resolver);
        }

        private IRelationMetadata GetMetadata(IMetadataBuilderContext context, MetadataIdentity identity)
        {
            MetadataIdentity parentIdentity = identity.Pop();
            IRelationMetadata parent = context.GetMetadata<IRelationMetadata>(parentIdentity.Name) ?? this.GetMetadata(context, parentIdentity);

            if (parent == null)
                return null;
            else if (parent.Item != null && parent.Item.Identity.Equals(identity))
                return parent.Item;

            return parent.Properties.FirstOrDefault(m => m.Identity.Equals(identity));
        }

        public void Initialize(IMetadataBuilderContext context)
        {
            RelationMetadata model = new RelationMetadata(context.Identity)
            {
                Flags = RelationMetadataFlags.Model | RelationMetadataFlags.Readable,
                Type = context.Schema.Model,
            };

            model.MemberOf = model;
            model.Properties = this.CreateLazy(() => this.CreateProperties(context, model));
            model.Depth = 0;

            model.Annotations = this.CreateAnnotations(model).ToList();
            model.Item = this.CreateItem(context, model);

            if (model.Item != null)
                model.Flags |= RelationMetadataFlags.List;

            context.AddMetadata<IRelationMetadata>(model);
        }

        private IRelationListContract GetListContract(RelationMetadata metadata)
        {
            IEnumerable<IRelationContractResolver> allResolvers = new[] { this.DefaultResolver }.Concat(this);

            IRelationListContract contract = allResolvers.Reverse().NotNull(cr => cr.GetListContract(metadata)).FirstOrDefault();

            if (contract != null)
                this.ValidateContract(metadata, contract);

            return contract;
        }

        private void ValidateContract(RelationMetadata metadata, IRelationListContract contract)
        {
            if (contract.ItemType == null)
                this.ThrowContractException(metadata, "Item type cannot be null.");
            else if (string.IsNullOrWhiteSpace(contract.ItemName))
                this.ThrowContractException(metadata, "Item name cannot be empty.");
            else
            {
                Type enumerableType = typeof(IEnumerable<>).MakeGenericType(contract.ItemType);

                if (!enumerableType.IsAssignableFrom(metadata.Type))
                    this.ThrowContractException(metadata, $"List of type '{metadata.Type.GetSanitizedName()}' cannot be converted to '{enumerableType.GetSanitizedName()}'.");
            }

            if (contract.ReadIndex != null && !contract.ReadIndex.HasSignature(contract.ItemType, typeof(int)))
                this.ThrowContractException(metadata, $"ReadIndex method must have signature '{contract.ItemType.GetSanitizedName()} (int)'.");

            if (contract.WriteIndex != null && !contract.WriteIndex.HasSignature(typeof(void), typeof(int), contract.ItemType))
                this.ThrowContractException(metadata, $"WriteIndex method must have signature 'void (int, {contract.ItemType.GetSanitizedName()})'.");
        }

        private void ThrowContractException(RelationMetadata metadata, string message)
        {
            throw new MetadataBuilderException($"Invalid contract for metadata '{metadata.Identity}'. {message}");
        }

        private Lazy<IReadOnlyList<TItem>> CreateLazy<TItem>(Func<IEnumerable<TItem>> factory) => new Lazy<IReadOnlyList<TItem>>(() => factory().ToList());

        private IEnumerable<Attribute> CreateAnnotations(RelationMetadata metadata)
        {
            IEnumerable<IRelationContractResolver> allResolvers = new[] { this.DefaultResolver }.Concat(this);

            return allResolvers.NotNull().SelectMany(cr => cr.GetAnnotationContract(metadata) ?? Array.Empty<Attribute>()).NotNull();
        }

        private IEnumerable<RelationMetadata> CreateProperties(IMetadataBuilderContext context, RelationMetadata parent)
        {
            IEnumerable<MemberInfo> members = parent.Type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (MemberInfo member in members.Where(m => this.IsFieldOrNonIndexedProperty(m)))
            {
                RelationMetadata property = this.CreateProperty(context, parent, member);

                if (property != null)
                    yield return property;
            }
                
        }

        private RelationMetadata CreateItem(IMetadataBuilderContext context, RelationMetadata parent)
        {
            IRelationListContract contract = this.GetListContract(parent);

            if (contract == null)
                return null;

            MetadataIdentity itemIdentity = parent.Identity.Push(contract.ItemName ?? "Item");

            RelationMetadata metadata = new RelationMetadata(itemIdentity)
            {
                Parent = parent,
                Type = contract.ItemType,
                Flags = RelationMetadataFlags.Item | RelationMetadataFlags.Property,
                ReadIndex = contract.ReadIndex,
                WriteIndex = contract.WriteIndex,
                Depth = parent.Depth + 1,
            };

            metadata.MemberOf = metadata;
            metadata.Item = this.CreateItem(context, metadata);
            metadata.Properties = this.CreateLazy(() => this.CreateProperties(context, metadata));
            metadata.Annotations = this.CreateAnnotations(metadata).ToList();

            if (this.IsMetadataRecursive(metadata))
                metadata.Flags |= RelationMetadataFlags.Recursive;

            if (contract.ReadIndex != null)
                metadata.Flags |= RelationMetadataFlags.Readable;

            if (contract.WriteIndex != null)
                metadata.Flags |= RelationMetadataFlags.Writable;

            if (metadata.Item != null)
                metadata.Flags |= RelationMetadataFlags.List;

            context.AddMetadata<IRelationMetadata>(metadata);

            return metadata;
        }

        private RelationMetadata CreateProperty(IMetadataBuilderContext context, RelationMetadata parent, MemberInfo memberInfo)
        {
            MetadataIdentity attributeId = parent.Identity.Push(memberInfo.Name);

            RelationMetadata metadata = new RelationMetadata(attributeId)
            {
                Type = this.GetMemberType(memberInfo),
                Parent = parent,
                Member = memberInfo,
                MemberOf = parent.MemberOf,
                Flags = RelationMetadataFlags.Property,
                Depth = parent.Depth,
            };

            metadata.Item = this.CreateItem(context, metadata);
            metadata.Properties = this.CreateLazy(() => this.CreateProperties(context, metadata));
            metadata.Annotations = this.CreateAnnotations(metadata).ToList();

            if (metadata.Item != null)
                metadata.Flags |= RelationMetadataFlags.List;

            if (this.IsMetadataRecursive(metadata))
                metadata.Flags |= RelationMetadataFlags.Recursive;

            if (memberInfo is PropertyInfo pi)
            {
                if (pi.CanRead)
                    metadata.Flags |= RelationMetadataFlags.Readable;

                if (pi.CanWrite)
                    metadata.Flags |= RelationMetadataFlags.Writable;
            }
            else if (memberInfo is FieldInfo)
                metadata.Flags |= RelationMetadataFlags.Readable | RelationMetadataFlags.Writable;

            context.AddMetadata<IRelationMetadata>(metadata);

            return metadata;
        }

        private bool IsMetadataRecursive(IRelationMetadata metadata)
        {
            List<RecurseNode> path = new List<RecurseNode>();

            while (metadata != null)
            {
                path.Add(new RecurseNode(metadata.Member, metadata.Type));

                metadata = metadata.Parent;
            }

            return !path.Distinct().SequenceEqual(path);
        }

        private bool IsMetadataRecursive2(IRelationMetadata metadata)
        {
            List<MemberInfo> propertyPath = new List<MemberInfo>();

            while (metadata != null)
            {
                if (metadata.Member != null)
                    propertyPath.Add(metadata.Member);

                metadata = metadata.Parent;
            }

            return !propertyPath.Distinct().SequenceEqual(propertyPath);
        }

        private bool IsFieldOrNonIndexedProperty(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo pi)
                return (pi.GetIndexParameters().Length == 0 && pi.GetAccessors(nonPublic: true).Any(m => m.IsAssembly || m.IsPublic));
            else if (memberInfo is FieldInfo fi)
                return fi.IsPublic;

            return false;
        }

        private Type GetMemberType(MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Property)
                return ((PropertyInfo)member).PropertyType;
            else if (member.MemberType == MemberTypes.Field)
                return ((FieldInfo)member).FieldType;

            return null;
        }

        private class RecurseNode : IEquatable<RecurseNode>
        {
            public MemberInfo Member { get; }
            public Type Type { get; }

            public RecurseNode(MemberInfo member, Type type)
            {
                this.Member = member;
                this.Type = type;
            }

            public bool Equals(RecurseNode other) => Equality.Combine(this, other, m => m.Member, m => m.Type);
            public override bool Equals(object obj) => (obj is RecurseNode other && this.Equals(other));
            public override int GetHashCode() => HashCode.Combine(this.Member, this.Type);
        }
    }
}
