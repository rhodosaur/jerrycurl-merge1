using Jerrycurl.Mvc.Test.Conventions.Accessors;
using Jerrycurl.Mvc.Test.Conventions2.NoDomain;
using Shouldly;

namespace Jerrycurl.Mvc.Test
{
    public class EngineTests
    {
        private readonly ProcLocator locator = new ProcLocator();
        private readonly ProcEngine engine = new ProcEngine(null);

        public void Test_Page_CanLookup_NoDomain()
        {
            PageDescriptor descriptor = this.locator.FindPage("NoDomainQuery", typeof(NoDomainAccessor));
            ProcArgs args = new ProcArgs(typeof(object), typeof(object));

            descriptor.ShouldNotBeNull();
            descriptor.DomainType.ShouldBeNull();

            PageFactory factory = Should.NotThrow(() => this.engine.Page(descriptor.PageType));

            factory.ShouldNotBeNull();
        }

        public void Test_Procedure_CannotLookup_NoDomain()
        {
            PageDescriptor descriptor = this.locator.FindPage("NoDomainQuery", typeof(NoDomainAccessor));
            ProcArgs args = new ProcArgs(typeof(object), typeof(object));

            descriptor.ShouldNotBeNull();
            descriptor.DomainType.ShouldBeNull();

            Should.Throw<ProcExecutionException>(() => this.engine.Proc(descriptor, args));
        }

        public void Test_Procedure_CanLookup()
        {
            PageDescriptor descriptor = this.FindPage("LocatorQuery2");
            ProcArgs args = new ProcArgs(typeof(int), typeof(object));

            ProcFactory factory = this.engine.Proc(descriptor, args);

            factory.ShouldNotBeNull();
        }

        public void Test_Page_ResultVariance()
        {
            PageDescriptor descriptor = this.FindPage("LocatorQuery2");
            ProcArgs args1 = new ProcArgs(typeof(object), typeof(int));
            ProcArgs args2 = new ProcArgs(typeof(object), typeof(string));

            this.engine.Proc(descriptor, args1).ShouldNotBeNull();
            this.engine.Proc(descriptor, args2).ShouldNotBeNull();
        }

        private PageDescriptor FindPage(string procName) => this.locator.FindPage(procName, typeof(LocatorAccessor));
    }
}
