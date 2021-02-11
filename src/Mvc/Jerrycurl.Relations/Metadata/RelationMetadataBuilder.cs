using System;
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
        public const int MaxRecursiveDepth = 1;

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

        private IRelationContract GetContract(RelationMetadata metadata)
        {
            IEnumerable<IRelationContractResolver> allResolvers = new[] { this.DefaultResolver }.Concat(this);

            IRelationContract contract = allResolvers.Reverse().NotNull(cr => cr.GetContract(metadata)).FirstOrDefault();

            if (contract != null)
                this.ValidateContract(metadata, contract);

            return contract;
        }

        private void ValidateContract(RelationMetadata metadata, IRelationContract contract)
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
            throw new MetadataBuilderException($"Invalid contract for {metadata.Identity}. {message}");
        }

        private Lazy<IReadOnlyList<TItem>> CreateLazy<TItem>(Func<IEnumerable<TItem>> factory) => new Lazy<IReadOnlyList<TItem>>(() => factory().ToList());

        private IEnumerable<Attribute> CreateAnnotations(RelationMetadata metadata)
        {
            IEnumerable<IRelationContractResolver> allResolvers = new[] { this.DefaultResolver }.Concat(this);

            return allResolvers.NotNull().SelectMany(cr => cr.GetAnnotations(metadata) ?? Array.Empty<Attribute>()).NotNull();
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
            metadata.Recursor = this.CreateRecursor(context, metadata);
            metadata.Properties = this.CreateLazy(() => this.CreateProperties(context, metadata));
            metadata.Annotations = this.CreateAnnotations(metadata).ToList();

            if (metadata.Item != null)
                metadata.Flags |= RelationMetadataFlags.List;

            if (metadata.Recursor != null)
                metadata.Flags |= RelationMetadataFlags.Recursive | RelationMetadataFlags.List;

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

            if (metadata.Item != null)
                context.AddMetadata<IRelationMetadata>(metadata.Item);

            return metadata;
        }

        private IRelationMetadata GetRecursiveParent(IMetadataBuilderContext context, RelationMetadata metadata)
        {
            IRelationMetadata current = metadata.Parent;
            IRelationMetadata stop = current.MemberOf.Parent ?? current.MemberOf;

            while (current != stop)
            {
                if (current.Type == metadata.Type)
                    return current;
            }

            return null;
        }

        private Lazy<IRelationMetadata> CreateRecursor(IMetadataBuilderContext context, RelationMetadata metadata)
        {
            if (metadata.HasFlag(RelationMetadataFlags.Item))
            {
                IRelationMetadata recursiveParent = this.GetRecursiveParent(context, metadata);

                if (recursiveParent != null)
                {
                    string recursivePath = context.Notation.Path(recursiveParent.Identity.Name, metadata.Parent.Identity.Name);
                    string otherPath = context.Notation.Combine(metadata.Identity.Name, recursivePath);

                    MetadataIdentity otherId = metadata.Identity.Push(recursivePath);

                    return new Lazy<IRelationMetadata>(() => this.GetMetadata(context, otherId));
                }
            }
            else if (metadata.MemberOf.Recursor != null)
            {
                IRelationContract contract = this.GetContract(metadata);

                if (contract != null && metadata.MemberOf.Type.Equals(contract.ItemType))
                    return new Lazy<IRelationMetadata>(() => metadata.MemberOf);
            }

            return null;
        }

        private RelationMetadata CreateItem(IMetadataBuilderContext context, RelationMetadata parent)
        {
            if (parent.MemberOf.HasFlag(RelationMetadataFlags.Recursive))
                return null;

            IRelationContract contract = this.GetContract(parent);

            if (contract == null)
                return null;

            MetadataIdentity itemIdentity = parent.Identity.Push(contract.ItemName ?? "Item");

            RelationMetadata metadata = new RelationMetadata(itemIdentity)
            {
                Parent = parent,
                Type = contract.ItemType,
                Flags = RelationMetadataFlags.Item,
                ReadIndex = contract.ReadIndex,
                WriteIndex = contract.WriteIndex,
                Depth = parent.Depth + 1,
            };

            metadata.MemberOf = metadata;
            metadata.Item = this.CreateItem(context, metadata);
            metadata.Recursor = this.CreateRecursor(context, metadata);
            metadata.Properties = this.CreateLazy(() => this.CreateProperties(context, metadata));
            metadata.Annotations = this.CreateAnnotations(metadata).ToList();

            if (contract.ReadIndex != null)
                metadata.Flags |= RelationMetadataFlags.Readable;

            if (contract.WriteIndex != null)
                metadata.Flags |= RelationMetadataFlags.Writable;

            if (metadata.Item != null)
                metadata.Flags |= RelationMetadataFlags.List;

            if (metadata.Recursor != null)
                metadata.Flags |= RelationMetadataFlags.Recursive;

            return metadata;
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
    }
}
