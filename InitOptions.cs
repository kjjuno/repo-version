using CommandLine;

namespace repo_version
{
    [Verb("init", HelpText = "Initializes a repository with a repo-version.json file.")]
    class InitOptions : OptionsBase
    { } 
}
