#tool nuget:?package=NuGet.CommandLine&version=6.2.1
#tool nuget:?package=GitVersion.CommandLine&version=5.10.3

#load "./build.local.cake"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Build-Test");
var gitHubToken = Argument("gitHubToken", EnvironmentVariable("GITHUB_TOKEN") ?? null);

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
    var settings = new DotNetNuGetPushSettings
    {
        Source = "https://nuget.pkg.github.com/zbeer/index.json",
        ApiKey = gitHubToken
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

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);