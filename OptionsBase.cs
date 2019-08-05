using CommandLine;

namespace repo_version
{
    class OptionsBase
    {
        [Value(0,
            MetaName = "path",
            Default = ".",
            HelpText = "Path to a git repository.")]
        public string Path { get; set; }
    }
}
