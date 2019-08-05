using CommandLine;

namespace repo_version
{
    [Verb("init", HelpText = "Initializes a repository with a repo-version.json file.")]
    class InitOptions
    {
        [Value(0,
            MetaName = "path",
            Default = ".",
            HelpText = "Path to a git repository.")]
        public string Path { get; set; }
    }
}
