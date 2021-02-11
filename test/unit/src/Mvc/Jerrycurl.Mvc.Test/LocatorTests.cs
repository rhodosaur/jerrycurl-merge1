using Jerrycurl.Mvc.Test.Conventions.Accessors;
using Shouldly;
using System;

namespace Jerrycurl.Mvc.Test
{
    public class LocatorTests
    {
        private readonly ProcLocator locator = new ProcLocator();
        private readonly Type accessorType = typeof(LocatorAccessor);

        public void Test_FindPage_CaseInsensitive()
        {
            PageDescriptor query = this.FindPage("locatorquery2");

            query.PageType.ShouldBe(typeof(Conventions.Queries.Locator.LocatorQuery2_cssql));
        }

        public void Test_FindPage_FromAccessor()
        {
            PageDescriptor query = this.FindPage("LocatorQuery2");
            PageDescriptor command = this.FindPage("LocatorCommand1");

            query.PageType.ShouldBe(typeof(Conventions.Queries.Locator.LocatorQuery2_cssql));
            command.PageType.ShouldBe(typeof(Conventions.Commands.Locator.LocatorCommand1_cssql));
        }

        public void Test_FindPage_FromPage()
        {
            PageDescriptor query = this.FindPage("LocatorQuery4", typeof(Conventions.Queries.Locator.LocatorQuery2_cssql));
            PageDescriptor command = this.FindPage("LocatorCommand3", typeof(Conventions.Commands.Locator.LocatorCommand1_cssql));

            query.PageType.ShouldBe(typeof(Conventions.Queries.LocatorQuery4_cssql));
            command.PageType.ShouldBe(typeof(Conventions.Commands.LocatorCommand3_cssql));
        }

        public void Test_FindPage_InSharedRoot()
        {
            PageDescriptor query = this.FindPage("LocatorQuery4");
            PageDescriptor command = this.FindPage("LocatorCommand3");

            query.PageType.ShouldBe(typeof(Conventions.Queries.LocatorQuery4_cssql));
            command.PageType.ShouldBe(typeof(Conventions.Commands.LocatorCommand3_cssql));
        }

        public void Test_FindPage_InSubFolder()
        {
            PageDescriptor query = this.FindPage("SubFolder1/SubFolder2/LocatorQuery1");

            query.PageType.ShouldBe(typeof(Conventions.Queries.Locator.SubFolder1.SubFolder2.LocatorQuery1_cssql));
        }

        public void Test_FindPage_InSharedFolder()
        {
            PageDescriptor query = this.FindPage("LocatorQuery3");
            PageDescriptor command = this.FindPage("LocatorCommand2");

            query.PageType.ShouldBe(typeof(Conventions.Queries.Shared.LocatorQuery3_cssql));
            command.PageType.ShouldBe(typeof(Conventions.Commands.Shared.LocatorCommand2_cssql));
        }

        public void Test_FindPage_NotExists()
        {
            Should.Throw<PageNotFoundException>(() => this.FindPage("LocatorQueryX", this.accessorType));
        }

        public void Test_FindPage_RelativePath()
        {
            PageDescriptor page = this.FindPage("../Queries/Locator/SubFolder1/./SubFolder2/../../LocatorQuery2");

            page.ShouldNotBeNull();
            page.PageType.ShouldBe(typeof(Conventions.Queries.Locator.LocatorQuery2_cssql));
        }

        public void Test_FindPage_AbsolutePath()
        {
            PageDescriptor page = this.FindPage("/Jerrycurl/Mvc/Test/Conventions/Queries/Locator/LocatorQuery2.cssql");

            page.ShouldNotBeNull();
            page.PageType.ShouldBe(typeof(Conventions.Queries.Locator.LocatorQuery2_cssql));
        }
        public void Test_FindPage_DomainPath()
        {
            PageDescriptor page = this.FindPage("~/Queries/Locator/LocatorQuery2.cssql");

            page.ShouldNotBeNull();
            page.PageType.ShouldBe(typeof(Conventions.Queries.Locator.LocatorQuery2_cssql));
        }

        private PageDescriptor FindPage(string procName, Type originType = null) => this.locator.FindPage(procName, originType ?? this.accessorType);
    }
}
