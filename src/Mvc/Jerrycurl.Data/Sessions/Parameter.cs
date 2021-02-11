using Jerrycurl.Data.Metadata;
using Jerrycurl.Relations;
using System;
using System.Data;

namespace Jerrycurl.Data.Sessions
{
    public class Parameter : IParameter
    {
        public string Name { get; }
        public IField Source { get; }

        public Parameter(string name, IField source = null)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Source = source;
        }

        [Obsolete("Should we use Snapshot or Data.Value?")]
        public void Build(IDbDataParameter adoParameter)
        {
            IBindingMetadata metadata = this.Source?.Identity.Metadata.Require<IBindingMetadata>();
            IBindingParameterContract contract = metadata?.Parameter;

            adoParameter.ParameterName = this.Name;

            if (contract?.Convert != null)
                adoParameter.Value = contract.Convert(this.Source?.Snapshot);
            else if (this.Source != null)
                adoParameter.Value = this.Source.Snapshot;
            else
                adoParameter.Value = DBNull.Value;

            if (contract?.Write != null)
            {
                BindingParameterInfo paramInfo = new BindingParameterInfo()
                {
                    Metadata = metadata,
                    Parameter = adoParameter,
                    Field = this.Source,
                };

                contract.Write(paramInfo);
            }
        }

        public override string ToString() => this.Name;
    }
}
