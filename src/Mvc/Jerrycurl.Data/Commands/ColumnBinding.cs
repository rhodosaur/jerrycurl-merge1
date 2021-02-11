using System;
using Jerrycurl.Relations;

namespace Jerrycurl.Data.Commands
{
    public class ColumnBinding : IUpdateBinding
    {
        public string ColumnName { get; }
        public IField2 Target { get; }

        public ColumnBinding(string columnName, IField2 target)
        {
            this.ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public ColumnBinding(IField2 target)
        {
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
            this.ColumnName = this.Target.Identity.Name;
        }

        public override string ToString() => $"ColumnBinding: {this.ColumnName} -> {this.Target.Identity.Name}";
    }
}
