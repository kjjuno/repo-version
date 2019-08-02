#addin nuget:?package=Cake.Json&version=4.0.0
#addin nuget:?package=Newtonsoft.Json&version=11.0.2

var target = Argument("target", "Pack");

string version = "0.0.1";

Setup(context =>
{
    IEnumerable<string> redirectedStandardOutput;
    var exitCodeWithArgument = StartProcess("dotnet", new ProcessSettings 
        {
            Arguments = "run --output json",
            RedirectStandardOutput = true
        },
        out redirectedStandardOutput);

    var json = string.Join("\n", redirectedStandardOutput);

    Information(json);

    var repoVersion = ParseJson(json);

    version = repoVersion["SemVer"].ToString();
});

Task("Pack")
    .Does(() =>
    {

        DotNetCorePack(".", new DotNetCorePackSettings
            {
                Configuration = "Release",
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["VERSION"] = version
                }
            });

    });

Task("Publish")
    .IsDependentOn("Pack")
    .Does(() =>
    {
        IEnumerable<string> redirectedStandardOutput;
        var exitCodeWithArgument = StartProcess("git", new ProcessSettings 
            {
                Arguments = "rev-parse --abbrev-ref HEAD",
                RedirectStandardOutput = true
            },
            out redirectedStandardOutput);

        var branch = redirectedStandardOutput.FirstOrDefault();

        Information($"Current Branch: {branch}");

        if (branch != "master")
        {
            Information("Not on master branch, no package will be pushed");
            return;
        }

        var apiKey = EnvironmentVariable("NUGET_API_KEY");

        Information($"NUGET_API_KEY: {apiKey}");

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("No value for NUGET_API_KEY");
        }

         var settings = new DotNetCoreNuGetPushSettings
         {
             ApiKey = apiKey
         };

         DotNetCoreNuGetPush($"nupkg/repo-version.{version}.nupkg", settings);
    });

RunTarget(target);
