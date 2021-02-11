using System.Threading.Tasks;
using Shouldly;
using Jerrycurl.Test;
using Jerrycurl.Data.Test.Model;
using Jerrycurl.Data.Queries;
using Jerrycurl.Data.Language;
using Jerrycurl.Relations.Language;
using Jerrycurl.Data.Metadata;
using System.Linq;

namespace Jerrycurl.Data.Test
{
    public class MetadataTests
    {
        public void Test_ReferenceMetadata_Keys()
        {
            var store = DatabaseHelper.Default.Schemas;
            var schema = store.GetSchema<Blog>();

            var blogKeys = schema.Require<IReferenceMetadata>().Keys;
            var postKeys = schema.Require<IReferenceMetadata>("Posts.Item").Keys;

            var blogPk = blogKeys.First(k => k.Name == "PK_Blog");
            var blogNpk = blogKeys.First(k => k.Name == "PK_Blog_2");

            var postFk1 = postKeys.First(k => k.Other == "PK_Blog");
            var postFk2 = postKeys.First(k => k.Other == "PK_Blog_2");

            blogPk.Flags.ShouldBe(ReferenceKeyFlags.Primary);
            blogNpk.Flags.ShouldBe(ReferenceKeyFlags.Candidate);

            postFk1.Flags.ShouldBe(ReferenceKeyFlags.Foreign);
            postFk2.Flags.ShouldBe(ReferenceKeyFlags.Foreign);
        }

        public void Test_ReferenceMetadata_References()
        {
            var store = DatabaseHelper.Default.Schemas;
            var schema = store.GetSchema<Blog>();

            var blogRefs = schema.Require<IReferenceMetadata>().References;
            var postRefs = schema.Require<IReferenceMetadata>("Posts.Item").References;

            var blogPr = blogRefs.First(r => r.Key.Name == "PK_Blog");
            var blogNpr = blogRefs.First(r => r.Key.Name == "PK_Blog_2");

            var postsPr = blogRefs.First(r => r.Key.Other == "PK_Blog");
            var postsNpr = blogRefs.First(r => r.Key.Other == "PK_Blog_2");

            blogPr.HasFlag(ReferenceFlags.Primary).ShouldBeTrue();
            blogNpr.HasFlag(ReferenceFlags.Candidate).ShouldBeTrue();
            blogNpr.HasFlag(ReferenceFlags.Primary).ShouldBeFalse();

            postsPr.HasFlag(ReferenceFlags.Foreign).ShouldBeTrue();
            postsNpr.HasFlag(ReferenceFlags.Foreign).ShouldBeTrue();

            blogPr.Other.ShouldBe(postsPr);
            blogNpr.Other.ShouldBe(postsNpr);
        }
    }
}
