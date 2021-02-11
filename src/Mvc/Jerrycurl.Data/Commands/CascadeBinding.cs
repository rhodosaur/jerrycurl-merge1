using System;
using Jerrycurl.Relations;

namespace Jerrycurl.Data.Commands
{
    public class CascadeBinding : IUpdateBinding
    {
        public IField2 Source { get; }
        public IField2 Target { get; }

        public CascadeBinding(IField2 source, IField2 target)
        {
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public override string ToString() => $"ParameterBinding: {this.Source.Identity.Name} -> {this.Target.Identity.Name}";
    }
}
