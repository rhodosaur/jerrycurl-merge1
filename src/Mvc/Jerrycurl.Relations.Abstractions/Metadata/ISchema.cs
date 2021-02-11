using System;

namespace Jerrycurl.Relations.Metadata
{
    public interface ISchema : IEquatable<ISchema>
    {
        Type Model { get; }
        IMetadataNotation Notation { get; }

        TMetadata Lookup<TMetadata>(string name) where TMetadata : IMetadata;
        TMetadata Lookup<TMetadata>() where TMetadata : IMetadata;

        TMetadata Require<TMetadata>(string name) where TMetadata : IMetadata;
        TMetadata Require<TMetadata>() where TMetadata : IMetadata;
    }
}