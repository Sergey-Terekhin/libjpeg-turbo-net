#addin "Cake.DocFx"
//#addin "Cake.Incubator"
#tool "docfx.console"

#tool nuget:?package=vswhere

DirectoryPath vsLatest  = VSWhereLatest();
FilePath msBuildPathX64 = (vsLatest==null)
                            ? null
                            : vsLatest.CombineWithFilePath("./MSBuild/15.0/Bin/amd64/MSBuild.exe");


//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDir = Directory("./bin") + Directory(configuration);
// dir to copy native libs
var nativesDir = buildDir + Directory("native");
//temp dir to build nuget packages
var nuget_temp_Dir = buildDir + Directory("nuget_temp");
//directory where nuget packages will be published
var nugetFeedDir = buildDir + Directory("nuget");    
//directory where assemblies will be published with all required dependencies
var publishDir = buildDir + Directory("assemblies"); 
var solution = "./libjpeg-turbo-net.sln";
//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
	var msBuildDir = System.IO.Path.GetDirectoryName(msBuildPathX64.ToString());
	Information("MSBuild dir: {0}",msBuildDir);
    NuGetRestore(solution, new NuGetRestoreSettings
	{
		ArgumentCustomization = args=>args.Append(string.Format("-MSBuildPath \"{0}\"", msBuildDir))
	});
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
      // Use MSBuild
		MSBuild(solution, new MSBuildSettings {
			Configuration = configuration,
			ToolPath = msBuildPathX64
		});
      //MSBuild(solution, settings => settings.SetConfiguration());
    }
    else
    {
      // Use XBuild
      XBuild(solution, settings =>
        settings.SetConfiguration(configuration));
    }
});

Task("Pack")
    .IsDependentOn("Build")
    .Does(()=>{
        var settings = new DotNetCorePackSettings
        {
            Configuration = configuration,
            OutputDirectory = nuget_temp_Dir,
        };
        PackProjects("./libjpeg-turbo-net/", settings);
        PackProjects("./libjpeg-turbo-native-win/", settings);

        Information("Initializing NuGet Local Feed at {0}", nugetFeedDir);
        NuGetInit(nuget_temp_Dir, nugetFeedDir);
        
        DeleteDirectory(nuget_temp_Dir, true);
    });

Task("Publish")
    .IsDependentOn("Build")
    .Does(()=>{
        PublishProjects("./libjpeg-turbo-net/", configuration, publishDir);
        PublishProjects("./libjpeg-turbo-native-win/", configuration, publishDir);
    });
	
//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Pack")
    .IsDependentOn("Publish");



//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);


private void PackProjects(string dir, DotNetCorePackSettings settings)
{
    foreach (var projectFile in System.IO.Directory.EnumerateFiles(dir, "*.csproj", System.IO.SearchOption.AllDirectories))
    {
		var isNetStandartProject =  XmlPeek(projectFile, "/Project/@Sdk") != null;
		if (!isNetStandartProject)
		{
			//it is full msbuild csproj not core
			continue;
		}
        Information("Packing project {0}", projectFile);
        DotNetCorePack(projectFile, settings);
    }
}
private void PublishProjects(string dir, string configuration, string rootDir)
{
    foreach (var projectFile in System.IO.Directory.EnumerateFiles(dir, "*.csproj", System.IO.SearchOption.AllDirectories))
    {
		var isNetStandartProject =  XmlPeek(projectFile, "/Project/@Sdk") != null;
		if (!isNetStandartProject)
		{
			//it is full msbuild csproj not core
			continue;
		}
        var targetFramework = XmlPeek(projectFile, "/Project/PropertyGroup/TargetFramework/text()");

        if (targetFramework == null)
        {
            targetFramework = XmlPeek(projectFile, "/Project/PropertyGroup/TargetFrameworks/text()");
        }

        var targetFrameworks = targetFramework.Split(';');

        foreach(var framework in targetFrameworks)
        {
            Information("Publishing project {0} for framework {1}", projectFile, framework);
            
            var settings = new DotNetCorePublishSettings
            {
                Configuration = configuration,
                OutputDirectory = Directory(rootDir) + Directory(framework),
                Framework = framework
            };

            DotNetCorePublish(projectFile, settings);
        }

    }
}
