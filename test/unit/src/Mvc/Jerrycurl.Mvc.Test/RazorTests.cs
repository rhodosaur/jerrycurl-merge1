using Jerrycurl.Mvc.Test.Conventions.Accessors;
using Jerrycurl.Mvc.Test.Conventions.Models;
using Jerrycurl.Test.Project.Models;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Test.Models;
using Jerrycurl.Mvc.Sql;
using Jerrycurl.Test.Project.Accessors;

namespace Jerrycurl.Mvc.Test
{
    public class RazorTests
    {
        public void Test_Razor_Vars()
        {
            
        }

        public void Test_Razor_Vals()
        {

        }

        public void Test_Razor_Props()
        {

        }

        public void Test_Razor_Filters()
        {

        }

        public void Test_Razor_Pars()
        {

        }
        public void Test_Razor_Eq()
        {

        }
        public void Test_Razor_IsEq()
        {

        }
        public void Test_Razor_Nulls()
        {

        }

        public void Test_Razor_As()
        {

        }

        public void Test_Razor_Open()
        {

        }

        public void Test_Razor_Attr()
        {

        }

        public void Test_Razor_For()
        {

        }

        public void Test_Razor_JsonPath()
        {

        }

        public void Test_Razor_Lits()
        {

        }

        public void Test_Razor_Cols()
        {

        }

        public void Test_Razor_ColNames()
        {
            //var runner = new Runner();
            //var run = new Runnable<object, XBlogView>();

            //run.R(p => p.ColName(m => m.Title));
            //run.Sql(";");
            //run.R(p => p.ColNames());
            //run.Sql(";");
            //run.R(p => p.ColNames(m => m.Posts));

            //var sql = runner.Sql(run);

            //sql.ShouldBe(@"""Title"";""Id"",""Title"";""Id"",""BlogId"",""CreatedOn"",""Headline"",""Content""");
        }

        public void Test_Razor_Tbls()
        {
            //var runner = new Runner();
            //var run = new Runnable<object, XBlogView>();

            //run.R(p => p.Tbl());
            //run.Sql(",");
            //run.R(p => p.Tbl(m => m.Posts));
            //run.Sql(",");
            //run.R(p => p.Open(m => m.Posts).Tbl());

            //var sql = runner.Sql(run);

            //sql.ShouldBe(@"""XBlog"" T0,""XBlogPost"" T1,""XBlogPost"" T1");
        }

        public void Test_Razor_TblNames()
        {
            //var runner = new Runner();
            //var run = new Runnable<object, XBlogView>();

            //run.R(p => p.TblName());
            //run.Sql(",");
            //run.R(p => p.TblName(m => m.Posts));
            //run.Sql(",");
            //run.R(p => p.Open(m => m.Posts).TblName());

            //var sql = runner.Sql(run);

            //sql.ShouldBe(@"""XBlog"",""XBlogPost"",""XBlogPost""");
        }

        public void Test_Razor_Star()
        {
//            var runner = new Runner();
//            var run = new Runnable<object, List<XBlog>>();

//            run.R(p => p.Star());

//            var sql = runner.Sql(run);

//            sql.ShouldBe(@"T0.""Id"" AS ""Item.Id"",
//T0.""Title"" AS ""Item.Title"",
//T0.""AuthorId"" AS ""Item.AuthorId""");
        }
    }
}
