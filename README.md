[![NuGet](https://img.shields.io/nuget/v/Jerrycurl)](https://nuget.org/packages/Jerrycurl)
[![Build status](https://ci.appveyor.com/api/projects/status/aihogw33ef50go9r?svg=true)](https://ci.appveyor.com/project/rhodosaur/jerrycurl/branch/master)
[![Test status](https://img.shields.io/appveyor/tests/rhodosaur/jerrycurl/dev)](https://ci.appveyor.com/project/rhodosaur/jerrycurl/branch/master/tests)
[![Gitter chat](https://badges.gitter.im/gitterHQ/gitter.png)](https://gitter.im/jerrycurl-mvc/community)
# Jerrycurl

**Jerrycurl** is an object-relational mapper and MVC framework that allows developers to build data access for .NET using tools and features inspired by those of ASP.NET MVC.

### Installation
Jerrycurl can be installed into any [SDK-style](https://docs.microsoft.com/en-us/nuget/resources/check-project-format) C# project from NuGet. Its main package contains support for compiling `.cssql` files into your project and executing them via the built-in MVC engine. Additionally you can install support for [one of our supported databases](https://nuget.org/packages?q=Jerrycurl.Vendors) from NuGet as well.

```shell
> dotnet add package Jerrycurl
> dotnet add package Jerrycurl.Vendors.SqlServer
```

#### Tooling
If you want to generate a ready-to-go object model from your database, install our [CLI](https://www.nuget.org/packages/dotnet-jerry/) from NuGet.
```shell
> dotnet tool install --global dotnet-jerry
You can invoke the tool using the following command: jerry
Tool 'dotnet-jerry' (version '1.1.0') was successfully installed.

> jerry scaffold -v sqlserver -c "SERVER=.;DATABASE=blogdb;TRUSTED_CONNECTION=true" -ns BlogDb.Database
Connecting to database 'blogdb'...
Generating...
Generated 7 tables and 21 columns in Database.cs.
```
To learn more about the CLI, type in `jerry help`.

### MVC design
Like ASP.NET, Jerrycurl features a design process that uses variant of the model-view-controller pattern made for the relational world. Each project comprises a selection
of models, accessors and procedures which are further grouped into either queries or commands, as per the CQS pattern.

#### Model layer
The model layer is a collection of simple data records and provides the structure for interacting with data and metadata on either the object side or the database side. It usually combined a representation of the database design in a familiar class-per-table manner along with customized views of different subsets of this data.

```csharp
// Database.cs
[Table("dbo", "Blog")]
class Blog
{
    [Id, Key("PK_Blog")]
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime CreatedOn { get; set; }
}
```
```csharp
// Views/Movies/MovieTaglineView.cs
class MovieTaglineView : Movie
{
    public string Tagline { get; set; }
}
```
```csharp
// Views/Movies/MovieRolesView.cs
class MovieRolesView : Movie
{
    public IList<MovieRole> Roles { get; set; }
}
```

#### Command/query layer
Commands and queries are written with our customized Razor SQL syntax and placed in `.cssql` files. They are placed in either the `Queries` or `Commands` folders based on whether they *read* or *write* data in the underlying database.
```
-- Queries/Blogs/GetAll.cssql
@result Blog
@model BlogFilter

SELECT     @R.Star()
FROM       @R.Tbl()
WHERE      @R.Col(m => m.CreatedOn) >= @M.Par(m => m.FromDate)
```
```
-- Commands/Movies/AddBlogs.cssql
@model Blog

@foreach (var v in this.M.Vals())
{
    INSERT INTO @v.TblName() ( @v.In().ColNames() )
    OUTPUT      @v.Out().Cols("INSERTED").As().Props()
    VALUES                   ( @v.In().Pars() )
}
```

#### Accessor (controller) layer
Accessors provide the bridge between the model and command/query layer by passing *input data* to an underlying `cssql` files and returning the *output data* a either instantiations of new objects (queries) or modifications to existing objects (commands). 
```csharp
// Accessors/BlogsAccessor.cs
public class BlogsAccessor : Accessor
{
    public IList<Blog> GetAll(DateTime fromDate) // -> Queries/Blogs/GetAll.cssql
        => this.Query<Blog>(model: new BlogFilter { FromDate = fromDate });
    
    public void AddBlogs(IList<Blog> newBlogs) // -> Commands/Blogs/AddBlogs.cssql
        => this.Execute(model: newMovies);
}
```

#### Domain (application) layer
A domain is created in a namespace parent to that of the accessor layer and presents a single, shared configuration for all operations in your project.
```csharp
// BlogsDomain.cs
class BlogsDomain : IDomain
{
    public void Configure(DomainOptions options)
    {
        options.UseSqlServer("SERVER=.;DATABASE=blogdb;TRUSTED_CONNECTION=true");
    }
}
```

To learn more about Jerrycurl and how to get started, visit [our official site](https://jerrycurl.net) or check our [samples repo](https://github.com/rwredding/jerrycurl-samples).

## Building from source
Jerrycurl can be built on [any OS supported by .NET Core](https://docs.microsoft.com/en-us/dotnet/core/install/dependencies) and included in this repository is a [PowerShell script](build.ps1) that performs all build-related tasks.

### Prerequisites
* .NET Core SDK 5.0
* .NET Core Runtime 2.1 / 3.1 (to run tests)
* PowerShell 5.0+ (PowerShell Core on Linux/macOS) 
* Visual Studio 2019 (latest) (optional)
* Docker (optional - for running live database tests)

### Clone, Build and Test
Clone the repository and run our build script from PowerShell.
```powershell
PS> git clone https://github.com/rhodosaur/jerrycurl
PS> cd jerrycurl
PS> .\build.ps1 [-NoTest] [-NoPack]
```

This runs the `Restore`, `Clean`, `Build`, `[Test]` and `[Pack]` targets on `jerrycurl.sln` and places NuGet packages in `/artifacts/packages`. Each target can also be run manually, and in Visual Studio if preferred.

By default, the `Test` target skips any unit test that requires live running database server. To help you to include these, you can run our [`docker compose` script](test/tools/boot-dbs.ps1) to boot up instances of our supported databases.

```powershell
PS> .\test\tools\boot-dbs.ps1 up sqlserver,mysql,postgres,oracle
```

Please allow ~60 seconds for the databases to be ready after which you can re-run `build.ps1`; it will then automatically target the included databases instances. When done, you can tear everything down again.

```powershell
PS> .\test\tools\boot-dbs.ps1 down sqlserver,mysql,postgres,oracle
```

> If you already have an empty database running that can be used for testing, you can manually specify its connection string in the environment variable `JERRY_SQLSERVER_CONN`, `JERRY_MYSQL_CONN`, `JERRY_POSTGRES_CONN` or `JERRY_ORACLE_CONN`.

> Pulling the Oracle Database image requires that you are logged into Docker and have accepted their [terms of service](https://hub.docker.com/_/oracle-database-enterprise-edition).
