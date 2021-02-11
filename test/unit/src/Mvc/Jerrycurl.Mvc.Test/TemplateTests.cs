using Jerrycurl.Mvc.Test.Conventions.Accessors;
using Shouldly;
using System.Collections.Generic;

namespace Jerrycurl.Mvc.Test
{
    public class TemplateTests
    {
        public void Test_Procedure_Template()
        {
            var misc = new MiscAccessor();
            var result = misc.TemplatedQuery();

            result.ShouldBe(new[] { 1, 2, 3 });
        }

        public void Test_Partial_Template()
        {
            var misc = new MiscAccessor();
            var result = misc.PartialedQuery();

            result.ShouldBe(new[] { 1, 2, 3 });
        }
    }
}
