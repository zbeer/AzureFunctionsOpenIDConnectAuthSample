#tool nuget:?package=NuGet.CommandLine&version=6.2.1
#tool nuget:?package=GitVersion.CommandLine&version=5.10.3

#load "./build.local.cake"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Build-Test");
var gitHubToken = Argument("gitHubToken", EnvironmentVariable("GITHUB_TOKEN") ?? null);
var authUrl = Argument("authUrl", EnvironmentVariable("AUTH_URL") ?? null);
var audience = Argument("audience", EnvironmentVariable("AUDIENCE") ?? null);
var clientId = Argument("clientId", EnvironmentVariable("CLIENT_ID") ?? null);
var clientSecret = Argument("clientSecret", EnvironmentVariable("CLIENT_SECRET") ?? null);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    IEnumerable<DirectoryPath> binDirectories;
    IEnumerable<DirectoryPath> objDirectories;

    try
    {
        DotNetClean("../AzureFunctionsOpenIDConnectAuthSample.sln");
    }
    catch { }

    binDirectories = GetDirectories("../**/bin").Where(d => !d.FullPath.Contains("node_modules"));
    CleanDirectories(binDirectories);

    objDirectories = GetDirectories("../**/obj").Where(d => !d.FullPath.Contains("node_modules"));
    CleanDirectories(objDirectories);
});

Task("Build")
    .Does(() =>
{
    DotNetBuild("../AzureFunctionsOpenIDConnectAuthSample.sln");
});

Task("Test")
    .Does(() =>
{
    DotNetTest("../AzureFunctionsOpenIDConnectAuthSample.sln", new DotNetTestSettings()
    {
        NoBuild = true,
        Filter = "TestCategory!=Smoke"
    });
});

Task("Package")
    .Does(() =>
{
    GitVersion(new GitVersionSettings()
    {
        ArgumentCustomization = args => args.Prepend("/updateprojectfiles"),
        WorkingDirectory = ".."
    });

    DotNetPack("../OidcApiAuthorization/OidcApiAuthorization.csproj", new DotNetPackSettings()
    {
        OutputDirectory = "..//artifacts",
    });
});

Task("Push")
    .Does(() =>
{
    Information("GitHub token: " + gitHubToken);

    var settings = new DotNetNuGetPushSettings
    {
        Source = "https://nuget.pkg.github.com/zbeer/index.json",
        ApiKey = gitHubToken,
        Verbosity = DotNetVerbosity.Detailed,
    };

    foreach (var file in GetFiles("..\\artifacts\\*.nupkg"))
    {
        DotNetNuGetPush(file, settings);
    }
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Build-Test")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

Task("Build-Test-Package")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package");

Task("Build-Test-Package-Push")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package")
    .IsDependentOn("Push");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);