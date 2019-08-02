using CommandLine;

namespace repo_version
{
    class Options
    {
        [Option('o', "output",
            Default = "semver",
            HelpText = "The output format. Should be one of [semver, json]")]
        public string Format { get; set; }

        [Value(0,
            MetaName = "path",
            Default = ".",
            HelpText = "Path to a git repository.")]
        public string Path { get; set; }
    }
}
