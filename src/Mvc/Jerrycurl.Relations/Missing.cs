using Jerrycurl.Diagnostics;
using Jerrycurl.Relations.Metadata;
using System;
using System.Diagnostics;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Relations
{
    [DebuggerDisplay("{Identity.Name}: {ToString(),nq}")]
    internal class Missing2<TValue> : IField2
    {
        public FieldIdentity Identity { get; }
        public IField2 Model { get; }
        public FieldType2 Type { get; } = FieldType2.Missing;
        public IRelationMetadata Metadata { get; }
        public bool HasChanged => false;
        public IFieldData Data => null;
        public object Snapshot => default(TValue);
        public bool IsReadOnly => true;

        public Missing2(string name, IRelationMetadata metadata, IField2 model)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            this.Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            this.Identity = new FieldIdentity(metadata.Identity, name);
            this.Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void Commit() { }
        public void Rollback() { }
        public void Update(object value) => throw BindingException2.From(this, "Cannot update missing field.");

        public override string ToString() => "<missing>";

        #region " Equality "
        public bool Equals(IField2 other) => Equality.Combine(this, other, m => m.Model, m => m.Identity);
        public override bool Equals(object obj) => (obj is IField2 other && this.Equals(other));
        public override int GetHashCode() => HashCode.Combine(this.Model, this.Identity);
        #endregion
    }
}
