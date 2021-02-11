using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Data.Metadata;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Data.Metadata.Annotations;
using System.Net.Security;
using System.Runtime.InteropServices;
using Jerrycurl.Relations;

namespace Jerrycurl.Data.Metadata
{
    public class ReferenceMetadataBuilder : IMetadataBuilder<IReferenceMetadata>
    {
        public IReferenceMetadata GetMetadata(IMetadataBuilderContext context) => this.GetMetadata(context, context.Identity);

        private IReferenceMetadata GetMetadata(IMetadataBuilderContext context, MetadataIdentity identity)
        {
            MetadataIdentity parentIdentity = identity.Pop();
            IReferenceMetadata parent = context.GetMetadata<IReferenceMetadata>(parentIdentity.Name) ?? this.GetMetadata(context, parentIdentity);

            if (parent == null)
                return null;
            else if (parent.Item != null && parent.Item.Identity.Equals(identity))
                return parent.Item;

            return parent.Properties.FirstOrDefault(m => m.Identity.Equals(identity));
        }

        public void Initialize(IMetadataBuilderContext context)
        {
            IRelationMetadata relation = context.Schema.Require<IRelationMetadata>(context.Identity.Name);

            context.AddMetadata<IReferenceMetadata>(this.CreateBaseMetadata(context, relation, null));
        }

        private ReferenceMetadata CreateBaseMetadata(IMetadataBuilderContext context, IRelationMetadata attribute, ReferenceMetadata parent)
        {
            ReferenceMetadata metadata = new ReferenceMetadata(attribute)
            {
                Parent = parent,
            };

            metadata.Flags = this.CreateFlags(metadata);
            metadata.Properties = this.CreateLazy(() => this.CreateProperties(context, metadata));
            metadata.Keys = this.CreateLazy(() => this.CreateKeys(metadata));
            metadata.Item = this.CreateItem(context, metadata);
            metadata.ParentReferences = this.CreateLazy(() => this.CreateParentReferences(metadata).ToList());
            metadata.ChildReferences = this.CreateLazy(() => this.CreateChildReferences(metadata));

            return metadata;
        }

        private Lazy<IReadOnlyList<TItem>> CreateLazy<TItem>(Func<IEnumerable<TItem>> factory) => new Lazy<IReadOnlyList<TItem>>(() => factory().ToList());

        private ReferenceMetadata CreateItem(IMetadataBuilderContext context, ReferenceMetadata parent)
        {
            if (parent.Relation.Item != null)
            {
                ReferenceMetadata metadata = this.CreateBaseMetadata(context, parent.Relation.Item, parent);

                context.AddMetadata<IReferenceMetadata>(metadata);

                return metadata;
            }

            return null;
        }

        private ReferenceMetadataFlags CreateFlags(ReferenceMetadata parent)
        {
            ReferenceMetadataFlags flags = ReferenceMetadataFlags.None;

            if (parent.Relation.Annotations.OfType<KeyAttribute>().Any(k => k.IsPrimary))
                flags |= ReferenceMetadataFlags.PrimaryKey;
            else if (parent.Relation.Annotations.OfType<KeyAttribute>().Any())
                flags |= ReferenceMetadataFlags.CandidateKey;

            if (parent.Relation.Annotations.OfType<RefAttribute>().Any())
                flags |= ReferenceMetadataFlags.ForeignKey;

            return flags;

        }
        private IEnumerable<ReferenceMetadata> CreateProperties(IMetadataBuilderContext context, ReferenceMetadata parent)
        {
            foreach (IRelationMetadata attribute in parent.Relation.Properties)
            {
                ReferenceMetadata metadata = this.CreateBaseMetadata(context, attribute, parent);

                context.AddMetadata<IReferenceMetadata>(metadata);

                yield return metadata;
            }
        }

        private IEnumerable<ReferenceKey> CreateKeys(ReferenceMetadata parent)
        {
            if (this.IsNativeKeylessType(parent.Type))
                return Array.Empty<ReferenceKey>();

            List<(ReferenceMetadata Metadata, KeyAttribute Attribute, string KeyName)> keyMap = new List<(ReferenceMetadata, KeyAttribute, string)>();
            List<(ReferenceMetadata Metadata, RefAttribute Attribute, string ReferenceName, string KeyName)> refMap = new List<(ReferenceMetadata, RefAttribute, string, string)>();

            foreach (ReferenceMetadata property in parent.Properties.Value)
            {
                foreach (KeyAttribute keyAttr in property.Relation.Annotations.OfType<KeyAttribute>())
                {
                    string keyName = keyAttr.Name ?? property.Relation.Member?.Name ?? "";

                    keyMap.Add((property, keyAttr, keyName));
                }

                foreach (RefAttribute refAttr in property.Relation.Annotations.OfType<RefAttribute>())
                {
                    string refName = refAttr.Name;
                    string keyName = refAttr.KeyName ?? property.Relation.Member?.Name ?? "";

                    refMap.Add((property, refAttr, refName, keyName));
                }
            }

            IEnumerable<ReferenceKey> candidateKeys = keyMap.GroupBy(t => t.KeyName).Select(g => new ReferenceKey()
            {
                Flags = g.All(t => t.Attribute.IsPrimary) ? ReferenceKeyFlags.Primary : ReferenceKeyFlags.Candidate,
                Name = g.First().KeyName,
                Properties = g.OrderBy(t => t.Attribute.Index).Select(t => t.Metadata).ToList(),
            });

            IEnumerable<ReferenceKey> foreignKeys = refMap.GroupBy(t => (t.ReferenceName, t.KeyName)).Select(g => new ReferenceKey()
            {
                Flags = ReferenceKeyFlags.Foreign,
                Name = g.First().ReferenceName,
                Other = g.First().KeyName,
                Properties = g.OrderBy(t => t.Attribute.Index).Select(t => t.Metadata).ToList(),
            });

            return candidateKeys.Concat(foreignKeys);
        }

        private bool IsNativeKeylessType(Type type) => (type.Assembly == typeof(string).Assembly);

        private bool IsKeyMatch(IReferenceKey rightKey, IReferenceKey leftKey)
        {
            if (leftKey.Properties.Count != rightKey.Properties.Count)
                return false;

            bool leftIsCandidate = leftKey.HasFlag(ReferenceKeyFlags.Candidate);
            bool rightIsCandidate = rightKey.HasFlag(ReferenceKeyFlags.Candidate);

            bool leftIsForeign = leftKey.HasFlag(ReferenceKeyFlags.Foreign);
            bool rightIsForeign = rightKey.HasFlag(ReferenceKeyFlags.Foreign);

            IReferenceKey candidateKey, foreignKey;

            if (leftIsCandidate && rightIsForeign)
            {
                candidateKey = leftKey;
                foreignKey = rightKey;
            }
            else if (leftIsForeign && rightIsCandidate)
            {
                candidateKey = rightKey;
                foreignKey = leftKey;
            }
            else
                return false;

            return foreignKey.Other.Equals(candidateKey.Name, StringComparison.Ordinal);
        }

        private IEnumerable<ReferenceMetadata> GetPossibleParents(ReferenceMetadata metadata)
        {
            if (metadata.Parent != null)
            {
                if (!metadata.Parent.HasFlag(RelationMetadataFlags.List))
                    yield return metadata.Parent;
                else if (metadata.Parent.Parent != null)
                    yield return metadata.Parent.Parent;
            }
        }

        private IEnumerable<ReferenceKey> GetPossibleChildKeys(ReferenceMetadata parent)
        {
            IEnumerable<ReferenceKey> childKeys = parent.Properties.Value.SelectMany(a => a.Keys.Value);
            IEnumerable<ReferenceKey> itemKeys = parent.Properties.Value.Where(m => m.Item != null).SelectMany(a => a.Item.Keys.Value);
            IEnumerable<ReferenceKey> allKeys = childKeys.Concat(itemKeys);

            if (parent.Relation.HasFlag(RelationMetadataFlags.Recursive))
                allKeys = allKeys.Concat(parent.Keys.Value);

            return allKeys;
        }

        private IEnumerable<Reference> CreateChildReferences(ReferenceMetadata metadata)
        {
            foreach (Reference reference in this.GetPossibleParents(metadata).SelectMany(m => m.ParentReferences.Value))
            {
                if (reference.Other.Metadata.Equals(metadata))
                    yield return reference.Other;
            }
        }

        private bool HasOneAttribute(ReferenceMetadata metadata) => metadata.Annotations.OfType<OneAttribute>().Any();
        private bool IsOneType(ReferenceMetadata metadata)
        {
            if (!metadata.Type.IsGenericType)
                return false;

            Type openType = metadata.Type.GetGenericTypeDefinition();

            if (openType == typeof(One<>))
                return true;

            return false;
        }

        private IEnumerable<Reference> CreateParentReferences(ReferenceMetadata parent)
        {
            if (!parent.Keys.Value.Any())
                return Array.Empty<Reference>();

            IEnumerable<ReferenceKey> parentKeys = parent.Keys.Value;
            IEnumerable<ReferenceKey> childKeys = this.GetPossibleChildKeys(parent);

            List<Reference> references = new List<Reference>();

            foreach (ReferenceKey parentKey in parentKeys)
            {
                foreach (ReferenceKey childKey in childKeys)
                {
                    if (this.IsKeyMatch(childKey, parentKey))
                    {
                        ReferenceMetadata childMetadata = childKey.Properties.First().Parent;

                        Reference rightRef = new Reference()
                        {
                            Metadata = childMetadata,
                            Flags = ReferenceFlags.Child,
                            Key = childKey,
                        };

                        Reference leftRef = new Reference()
                        {
                            Metadata = parent,
                            Flags = ReferenceFlags.Parent | ReferenceFlags.One,
                            Key = parentKey,
                        };

                        if (childKey.HasFlag(ReferenceKeyFlags.Candidate))
                        {
                            rightRef.Flags |= childKey.HasFlag(ReferenceKeyFlags.Primary) ? ReferenceFlags.Primary : ReferenceFlags.Candidate;
                            leftRef.Flags |= ReferenceFlags.Foreign;

                            if (childKey.HasFlag(ReferenceKeyFlags.Primary))
                                rightRef.Flags |= ReferenceFlags.Primary;
                        }
                        else
                        {
                            rightRef.Flags |= ReferenceFlags.Foreign;
                            leftRef.Flags |= parentKey.HasFlag(ReferenceKeyFlags.Primary) ? ReferenceFlags.Primary : ReferenceFlags.Candidate;
                        }

                        if (childMetadata.Relation.HasFlag(RelationMetadataFlags.Item))
                        {
                            rightRef.List = leftRef.List = childMetadata.Parent;

                            if (this.IsOneType(childMetadata.Parent))
                            {
                                leftRef.Flags &= ~ReferenceFlags.One;
                                leftRef.Flags |= ReferenceFlags.Many;
                                rightRef.Flags |= ReferenceFlags.One;
                            }
                            else
                                rightRef.Flags |= ReferenceFlags.Many;
                        }
                        else if (this.HasOneAttribute(childMetadata))
                        {
                            leftRef.Flags &= ~ReferenceFlags.One;
                            leftRef.Flags |= ReferenceFlags.Many;
                        }

                        if (childMetadata.Equals(parent))
                        {
                            leftRef.Flags |= ReferenceFlags.Self | ReferenceFlags.Child;
                            rightRef.Flags |= ReferenceFlags.Self | ReferenceFlags.Parent;
                        }

                        leftRef.Other = rightRef;
                        rightRef.Other = leftRef;

                        references.Add(leftRef);
                    }
                }
            }

            return references;
        }
    }
}
