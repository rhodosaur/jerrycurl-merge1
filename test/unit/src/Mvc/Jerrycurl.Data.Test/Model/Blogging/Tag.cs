using Jerrycurl.Data.Metadata.Annotations;

namespace Jerrycurl.Data.Test.Model.Blogging
{
    internal class Tag
    {
        [Key("PK_Tag")]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
