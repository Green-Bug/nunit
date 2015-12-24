//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");
var framework = Argument("framework", "net-4.5");

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var version = "3.1.0";
var modifier = "";
var displayVersion = "3.1";

var isAppveyor = BuildSystem.IsRunningOnAppVeyor;
var dbgSuffix = configuration == "Debug" ? "-dbg" : "";
var packageVersion = version + modifier + dbgSuffix;

//////////////////////////////////////////////////////////////////////
// SUPPORTED FRAMEWORKS
//////////////////////////////////////////////////////////////////////

var WindowsFrameworks = new string[] {
	"net-4.5", "net-4.0", "net-2.0", "portable", "sl-5.0" };

var LinuxFrameworks = new string[] {
	"net-4.5", "net-4.0", "net-2.0" };

var AllFrameworks = IsRunningOnWindows() ? WindowsFrameworks : LinuxFrameworks;
	
//////////////////////////////////////////////////////////////////////
// DEFINE RUN CONSTANTS
//////////////////////////////////////////////////////////////////////

// Directories
var PACKAGE_DIR = "package/";
var BIN_DIR = "bin/" + configuration + "/";

// Test Runners
var NUNIT3_CONSOLE = BIN_DIR + "nunit3-console.exe";
var NUNITLITE_RUNNER = "nunitlite-runner.exe";

// Test Assemblies
var FRAMEWORK_TESTS = "nunit.framework.tests.dll";
var NUNITLITE_TESTS = "nunitlite.tests.exe";
var ENGINE_TESTS = "nunit.engine.tests.dll";
var ADDIN_TESTS = "addins/tests/addin-tests.dll";
var V2_DRIVER_TESTS = "addins/v2-tests/nunit.v2.driver.tests.dll";
var CONSOLE_TESTS = "nunit3-console.tests.dll";

// Packages
var SRC_PACKAGE = PACKAGE_DIR + "NUnit-" + version + modifier + "-src.zip";
var ZIP_PACKAGE = PACKAGE_DIR + "NUnit-" + packageVersion + ".zip";

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(BIN_DIR);
});


//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("InitializeBuild")
    .Does(() =>
{
    if (IsRunningOnWindows())
        NuGetRestore("./nunit.sln");
    else
        NuGetRestore("./nunit.linux.sln");

	if (BuildSystem.IsRunningOnAppVeyor)
	{
		var tag = AppVeyor.Environment.Repository.Tag;
		var buildNumber = AppVeyor.Environment.Build.Number;
		
		packageVersion = tag.IsTag ? tag.Name : version + "-CI-" + buildNumber + dbgSuffix;

		AppVeyor.UpdateBuildVersion(packageVersion);
	}
});

//////////////////////////////////////////////////////////////////////
// BUILD
//////////////////////////////////////////////////////////////////////

Task("BuildAllFrameworks")
    .IsDependentOn("InitializeBuild")
    .Does(() =>
    {
        foreach (var runtime in AllFrameworks)
            BuildFramework(configuration, runtime);
    });

Task("BuildFramework")
    .IsDependentOn("InitializeBuild")
    .Does(() =>
    {
        BuildFramework(configuration, framework);
    });

Task("BuildEngine")
    .IsDependentOn("InitializeBuild")
    .Does(() =>
    {
        BuildEngine(configuration);
    });

Task("BuildConsole")
    .IsDependentOn("InitializeBuild")
    .Does(() =>
    {
        BuildConsole(configuration);
    });
    
//////////////////////////////////////////////////////////////////////
// TEST
//////////////////////////////////////////////////////////////////////
Task("TestAllFrameworks")
  .IsDependentOn("Build")
	.Does(() =>
	{
		foreach(string runtime in AllFrameworks)
		{
			RunTest(BIN_DIR + File(runtime + "/" + NUNITLITE_RUNNER), BIN_DIR + "/" + runtime, FRAMEWORK_TESTS);

			if (runtime == "sl-5.0")
				RunTest(BIN_DIR + File(runtime + "/" + NUNITLITE_RUNNER), BIN_DIR + "/" + runtime, "nunitlite.tests.dll");
			else
				RunTest(BIN_DIR + File(runtime + "/" + NUNITLITE_TESTS), BIN_DIR);
		}
	});

Task("TestFramework")
  .IsDependentOn("Build")
	.Does(() => 
	{ 
		RunTest(BIN_DIR + File(framework + "/" + NUNITLITE_RUNNER), BIN_DIR + "/" + framework, FRAMEWORK_TESTS);
	});

Task("TestNUnitLite")
  .IsDependentOn("BuildFramework")
	.Does(() => 
	{ 
			if (framework == "sl-5.0")
				RunTest(BIN_DIR + File(framework + "/" + NUNITLITE_RUNNER), BIN_DIR + "/" + framework, "nunitlite.tests.dll");
			else
				RunTest(BIN_DIR + File(framework + "/" + NUNITLITE_TESTS), BIN_DIR);
	});

Task("TestEngine")
  .IsDependentOn("Build")
  .Does(() => 
	{ 
		RunTest(NUNIT3_CONSOLE, BIN_DIR, ENGINE_TESTS);
	});

Task("TestAddins")
  .IsDependentOn("Build")
  .Does(() => 
	{
		RunTest(NUNIT3_CONSOLE, BIN_DIR, ADDIN_TESTS); 
	});

Task("TestV2Driver")
  .IsDependentOn("Build")
  .Does(() => 
	{ 
		RunTest(NUNIT3_CONSOLE, BIN_DIR, V2_DRIVER_TESTS);
	});

Task("TestConsole")
  .IsDependentOn("Build")
  .Does(() => 
	{ 
		RunTest(NUNIT3_CONSOLE, BIN_DIR, CONSOLE_TESTS);
	});

//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

var RootFiles = new FilePath[]
{
	"LICENSE.txt",
	"NOTICES.txt",
	"CHANGES.txt", 
	"nunit.ico"
};

var BinFiles = new FilePath[]
{
	"ConsoleTests.nunit",
	"EngineTests.nunit",
	"mock-nunit-assembly.exe",
	"Mono.Cecil.dll",
	"nunit-agent-x86.exe",
	"nunit-agent-x86.exe.config",
	"nunit-agent.exe",
	"nunit-agent.exe.config",
	"nunit.engine.addin.xml",
	"nunit.engine.addins",
	"nunit.engine.api.dll",
	"nunit.engine.api.xml",
	"nunit.engine.dll",
	"nunit.engine.tests.dll",
	"nunit.engine.tests.dll.config",
	"nunit.framework.dll",
	"nunit.framework.xml",
	"NUnit2TestResult.xsd",
	"nunit3-console.exe",
	"nunit3-console.exe.config",
	"nunit3-console.tests.dll",
	"nunitlite.dll",
	"TextSummary.xslt",
	"addins/nunit-project-loader.dll",
	"addins/nunit-v2-result-writer.dll",
	"addins/nunit.core.dll",
	"addins/nunit.core.interfaces.dll",
	"addins/nunit.v2.driver.dll",
	"addins/vs-project-loader.dll",
	"addins/tests/addin-tests.dll",
	"addins/tests/nunit-project-loader.dll",
	"addins/tests/nunit.engine.api.dll",
	"addins/tests/nunit.engine.api.xml",
	"addins/tests/nunit.framework.dll",
	"addins/tests/nunit.framework.xml",
	"addins/tests/vs-project-loader.dll",
	"addins/v2-tests/nunit.framework.dll",
	"addins/v2-tests/nunit.framework.xml",
	"addins/v2-tests/nunit.v2.driver.tests.dll"
};

// Not all of these are present in every framework
var FrameworkFiles = new FilePath[]
{
	"AppManifest.xaml",
	"mock-assembly.dll",
	"mock-assembly.exe",
	"nunit.framework.dll",
	"nunit.framework.xml",
	"nunit.framework.tests.dll",
	"nunit.framework.tests.xap",
	"nunit.framework.tests_TestPage.html",
	"nunit.testdata.dll",
	"nunitlite.dll",
	"nunitlite.tests.exe",
	"nunitlite.tests.dll",
	"slow-nunit-tests.dll",
	"nunitlite-runner.exe"
};

Task("PackageSource")
  .Does(() =>
	{
		CreateDirectory(PACKAGE_DIR);
		RunGitCommand(string.Format("archive -o {0} HEAD", SRC_PACKAGE));
	});

Task("CreateImage")
	.Does(() =>
	{
		var currentImageDir = "images/NUnit-" + packageVersion + "/";
		var imageBinDir = currentImageDir + "bin/";

		CleanDirectory(currentImageDir);

		CopyFiles(RootFiles, currentImageDir);

		CreateDirectory(imageBinDir);

		foreach(FilePath file in BinFiles)
		{
		  if (FileExists(BIN_DIR + file))
		  {
			  CreateDirectory(imageBinDir + file.GetDirectory());
			  CopyFile(BIN_DIR + file, imageBinDir + file);
			}
		}			

		foreach (var runtime in AllFrameworks)
		{
			var targetDir = imageBinDir + Directory(runtime);
			var sourceDir = BIN_DIR + Directory(runtime);
			CreateDirectory(targetDir);
			foreach (FilePath file in FrameworkFiles)
			{
				var sourcePath = sourceDir + "/" + file;
				if (FileExists(sourcePath))
					CopyFileToDirectory(sourcePath, targetDir);
			}
		}
	});

Task("PackageZip")
  .IsDependentOn("CreateImage")
	.Does(() =>
	{
		var currentImageDir = "images/NUnit-" + packageVersion + "/";
		CreateDirectory(PACKAGE_DIR);
		Zip(MakeAbsolute(Directory(currentImageDir)), File(ZIP_PACKAGE));
	});

Task("PackageNuGet")
  .IsDependentOn("CreateImage")
	.Does(() =>
	{
		var currentImageDir = "images/NUnit-" + packageVersion + "/";

		CreateDirectory(PACKAGE_DIR);
		NuGetPack("nuget/nunit.nuspec", new NuGetPackSettings()
		{
			Version = packageVersion,
			BasePath = currentImageDir,
			OutputDirectory = PACKAGE_DIR
		});
		NuGetPack("nuget/nunitSL.nuspec", new NuGetPackSettings()
		{
			Version = packageVersion,
			BasePath = currentImageDir,
			OutputDirectory = PACKAGE_DIR
		});
		NuGetPack("nuget/nunitlite.nuspec", new NuGetPackSettings()
		{
			Version = packageVersion,
			BasePath = currentImageDir,
			OutputDirectory = PACKAGE_DIR
		});
		NuGetPack("nuget/nunitliteSL.nuspec", new NuGetPackSettings()
		{
			Version = packageVersion,
			BasePath = currentImageDir,
			OutputDirectory = PACKAGE_DIR
		});
		NuGetPack("nuget/nunit.console.nuspec", new NuGetPackSettings()
		{
			Version = packageVersion,
			BasePath = currentImageDir,
			OutputDirectory = PACKAGE_DIR,
			NoPackageAnalysis = true
		});
		NuGetPack("nuget/nunit.runners.nuspec", new NuGetPackSettings()
		{
			Version = packageVersion,
			BasePath = currentImageDir,
			OutputDirectory = PACKAGE_DIR,
			NoPackageAnalysis = true
		});
		NuGetPack("nuget/nunit.engine.nuspec", new NuGetPackSettings()
		{
			Version = packageVersion,
			BasePath = currentImageDir,
			OutputDirectory = PACKAGE_DIR,
			NoPackageAnalysis = true
		});
	});


//////////////////////////////////////////////////////////////////////
// HELPER METHODS
//////////////////////////////////////////////////////////////////////

void BuildFramework(string configuration, string framework)
{
	switch(framework)
	{
		case "net-4.5":
		case "net-4.0":
		case "net-2.0":
			var suffix = framework.Substring(4);
			BuildProject("src/NUnitFramework/framework/nunit.framework-" + suffix +".csproj", configuration);
			BuildProject("src/NUnitFramework/nunitlite/nunitlite-" + suffix +".csproj", configuration);
			BuildProject("src/NUnitFramework/mock-assembly/mock-assembly-" + suffix +".csproj", configuration);
			BuildProject("src/NUnitFramework/testdata/nunit.testdata-" + suffix +".csproj", configuration);
			BuildProject("src/NUnitFramework/slow-tests/slow-nunit-tests-" + suffix +".csproj", configuration);
			BuildProject("src/NUnitFramework/tests/nunit.framework.tests-" + suffix +".csproj", configuration);
			BuildProject("src/NUnitFramework/nunitlite.tests/nunitlite.tests-" + suffix +".csproj", configuration);
			BuildProject("src/NUnitFramework/nunitlite-runner/nunitlite-runner-" + suffix + ".csproj", configuration);
			break;

		case "portable":
			BuildProject("src/NUnitFramework/framework/nunit.framework-portable.csproj", configuration);
			BuildProject("src/NUnitFramework/nunitlite/nunitlite-portable.csproj", configuration);
			BuildProject("src/NUnitFramework/mock-assembly/mock-assembly-portable.csproj", configuration);
			BuildProject("src/NUnitFramework/testdata/nunit.testdata-portable.csproj", configuration);
			BuildProject("src/NUnitFramework/tests/nunit.framework.tests-portable.csproj", configuration);
			BuildProject("src/NUnitFramework/nunitlite.tests/nunitlite.tests-portable.csproj", configuration);
			BuildProject("src/NUnitFramework/nunitlite-runner/nunitlite-runner-portable.csproj", configuration);
			break;

		case "sl-5.0":
			BuildProject("src/NUnitFramework/framework/nunit.framework-sl-5.0.csproj", configuration, MSBuildPlatform.x86);
			BuildProject("src/NUnitFramework/nunitlite/nunitlite-sl-5.0.csproj", configuration, MSBuildPlatform.x86);
			BuildProject("src/NUnitFramework/mock-assembly/mock-assembly-sl-5.0.csproj", configuration, MSBuildPlatform.x86);
			BuildProject("src/NUnitFramework/testdata/nunit.testdata-sl-5.0.csproj", configuration, MSBuildPlatform.x86);
			BuildProject("src/NUnitFramework/tests/nunit.framework.tests-sl-5.0.csproj", configuration, MSBuildPlatform.x86);
			BuildProject("src/NUnitFramework/nunitlite.tests/nunitlite.tests-sl-5.0.csproj", configuration, MSBuildPlatform.x86);
			BuildProject("src/NUnitFramework/nunitlite-runner/nunitlite-runner-sl-5.0.csproj", configuration, MSBuildPlatform.x86);
			break;
	}
}

void BuildEngine(string configuration)
{
    BuildProject("./src/NUnitEngine/nunit.engine.api/nunit.engine.api.csproj", configuration);
    BuildProject("./src/NUnitEngine/nunit.engine/nunit.engine.csproj", configuration);
    BuildProject("./src/NUnitEngine/nunit-agent/nunit-agent.csproj", configuration);
    BuildProject("./src/NUnitEngine/nunit-agent/nunit-agent-x86.csproj", configuration);
    
    // Engine tests
    BuildProject("./src/NUnitEngine/nunit.engine.tests/nunit.engine.tests.csproj", configuration);  

    // Addins
    BuildProject("./src/NUnitEngine/Addins/nunit-project-loader/nunit-project-loader.csproj", configuration);  
    BuildProject("./src/NUnitEngine/Addins/vs-project-loader/vs-project-loader.csproj", configuration);  
    BuildProject("./src/NUnitEngine/Addins/nunit-v2-result-writer/nunit-v2-result-writer.csproj", configuration);
    BuildProject("./src/NUnitEngine/Addins/nunit.v2.driver/nunit.v2.driver.csproj", configuration);

    // Addin tests
    BuildProject("./src/NUnitEngine/Addins/addin-tests/addin-tests.csproj", configuration);
    BuildProject("./src/NUnitEngine/Addins/nunit.v2.driver.tests/nunit.v2.driver.tests.csproj", configuration);
}

void BuildConsole(string configuration)
{
    BuildProject("src/NUnitConsole/nunit3-console/nunit3-console.csproj", configuration);
    BuildProject("src/NUnitConsole/nunit3-console.tests/nunit3-console.tests.csproj", configuration);
}

void BuildProject(string projectPath, string configuration)
{
	BuildProject(projectPath, configuration, MSBuildPlatform.Automatic);
}

void BuildProject(string projectPath, string configuration, MSBuildPlatform buildPlatform)
{
    if(IsRunningOnWindows())
    {
        // Use MSBuild
        MSBuild(projectPath, new MSBuildSettings()
            .SetConfiguration(configuration)
			.SetMSBuildPlatform(buildPlatform)
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false)
        );
    }
    else
    {
        // Use XBuild
        XBuild(projectPath, new XBuildSettings()
            .WithTarget("Build")
            .WithProperty("Configuration", configuration)
            .SetVerbosity(Verbosity.Minimal)
        );
    }
}
 
void RunTest(FilePath exePath, DirectoryPath workingDir)
{
	int rc = StartProcess(
	  MakeAbsolute(exePath), 
	  new ProcessSettings()
	  {
		  WorkingDirectory = workingDir
	  });
	  	  
	if (rc > 0)
	  throw new Exception(string.Format("{0} tests failed", rc));
	else if (rc < 0)
	  throw new Exception(string.Format("{0} returned rc = {1}", exePath, rc));
}

void RunTest(FilePath exePath, DirectoryPath workingDir, string arguments)
{
	int rc = StartProcess(
	  MakeAbsolute(exePath), 
	  new ProcessSettings()
	  {
		  Arguments = arguments,
		  WorkingDirectory = workingDir
	  });
	  
	if (rc > 0)
	  throw new Exception(string.Format("{0} tests failed", rc));
	else if (rc < 0)
	  throw new Exception(string.Format("{0} returned rc = {1}", exePath, rc));
}

void RunGitCommand(string arguments)
{
	StartProcess("git", new ProcessSettings()
	{
		Arguments = arguments
	});
}

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Build")
  .IsDependentOn("BuildAllFrameworks")
  .IsDependentOn("BuildEngine")
  .IsDependentOn("BuildConsole");
  
Task("Rebuild")
  .IsDependentOn("Clean")
	.IsDependentOn("Build");

Task("TestAll")
	.IsDependentOn("TestAllFrameworks")
	.IsDependentOn("TestEngine")
	.IsDependentOn("TestAddins")
	.IsDependentOn("TestV2Driver")
	.IsDependentOn("TestConsole");

Task("Test")
	.IsDependentOn("TestFramework")
	.IsDependentOn("TestNUnitLite")
	.IsDependentOn("TestEngine")
	.IsDependentOn("TestAddins")
	.IsDependentOn("TestV2Driver")
	.IsDependentOn("TestConsole");

Task("Package")
	.IsDependentOn("PackageSource")
	.IsDependentOn("PackageZip")
	.IsDependentOn("PackageNuGet");

Task("Appveyor")
	.IsDependentOn("Build")
	.IsDependentOn("TestAll")
	.IsDependentOn("Package");

Task("Travis")
	.IsDependentOn("Build")
	.IsDependentOn("TestAll");

Task("Default")
    .IsDependentOn("Build"); // Rebuild?

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
