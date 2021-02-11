using System;
using Jerrycurl.Relations;

namespace Jerrycurl.Data.Commands
{
    public class CascadeBinding : IUpdateBinding
    {
        public IField2 Source { get; }
        public IField2 Target { get; }

        public CascadeBinding(IField2 target, IField2 source)
        {
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public override string ToString() => $"CascadeBinding: {this.Source.Identity} -> {this.Target.Identity}";
    }
}
