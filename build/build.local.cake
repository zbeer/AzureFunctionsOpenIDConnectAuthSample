#addin "Cake.Npm&version=2.0.0"
#addin "Cake.FileHelpers&version=5.0.0"

using System.Diagnostics;

//////////////////////////////////////////////////////////////////////
// GLOBALS
//////////////////////////////////////////////////////////////////////

FilePath _azureFunctionsCoreToolsExe = MakeAbsolute(File("./node_modules/azure-functions-core-tools/bin/func.exe"));
DirectoryPath _apiPath = new DirectoryPath("../SampleFunctionApp/bin/Debug/net6.0");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Install-Azure-Functions-Core-Tools")
    .Does(() =>
{
    FilePathCollection azureFunctionCliFiles;
    FilePath azureFunctionCliDll;
    FileVersionInfo azureFunctionCliDllFileVersionInfo;
    bool needToInstall = false;

    azureFunctionCliFiles = GetFiles("./node_modules/azure-functions-core-tools/**/func.dll");
    if (azureFunctionCliFiles.Count() > 0)
    {
        azureFunctionCliDll = azureFunctionCliFiles.First();
        azureFunctionCliDllFileVersionInfo = FileVersionInfo.GetVersionInfo(azureFunctionCliDll.ToString());
        if (azureFunctionCliDllFileVersionInfo.FileVersion != "4.0.4829")
            needToInstall = true;
    }
    else
        needToInstall = true;

    if (needToInstall)
    {
        NpmInstall(new NpmInstallSettings().AddPackage("azure-functions-core-tools", "4.0.4829")
                                           .InstallLocally());
    }
    else
    {
        Information("Azure Function Core Tools v4.0.4829 already installed.");
    }
});

Task("Local-Integration-Test")
    .Does(() =>
{
    foreach (FilePath testProject in GetFiles("../**/*SmokeTests.csproj"))
        DotNetBuild(testProject.FullPath);

    ReplaceTextInFiles("../**/bin/**/appSettings.json", "%API_URL%", "http://localhost:7071");
    ReplaceTextInFiles("../**/bin/**/appSettings.json", "%AUTH_URL%", authUrl);
    ReplaceTextInFiles("../**/bin/**/appSettings.json", "%AUDIENCE%", audience);
    ReplaceTextInFiles("../**/bin/**/appSettings.json", "%CLIENT_ID%", clientId);
    ReplaceTextInFiles("../**/bin/**/appSettings.json", "%CLIENT_SECRET%", clientSecret);

    ReplaceTextInFiles("../**/bin/**/local.Settings.json", "%AUDIENCE%", audience);
    ReplaceTextInFiles("../**/bin/**/local.Settings.json", "%AUTH_URL%", authUrl);

    using(var api = StartAndReturnProcess(_azureFunctionsCoreToolsExe,
                                          new ProcessSettings
                                          {
                                              WorkingDirectory = _apiPath,
                                              Arguments = "host start",
                                          }))
    {
        System.Threading.Thread.Sleep(3000);
        try
        {
            foreach (FilePath testProject in GetFiles("../**/*SmokeTests.csproj"))
            {
                DotNetTest(testProject.FullPath);
            }
        }
        finally
        {
            api.Kill();
        }
    }
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Run-Local-Smoke-Test")
    .IsDependentOn("Build-Test-Package")
    .IsDependentOn("Install-Azure-Functions-Core-Tools")
    .IsDependentOn("Local-Integration-Test");