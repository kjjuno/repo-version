#addin nuget:?package=Cake.Json&version=4.0.0
#addin nuget:?package=Newtonsoft.Json&version=11.0.2

var target = Argument("target", "Pack");

Task("Pack")
    .Does(() =>
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

        var version = repoVersion["SemVer"].ToString();

        DotNetCorePack(".", new DotNetCorePackSettings
            {
                Configuration = "Release",
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["VERSION"] = version
                }
            });
    });

RunTarget(target);
