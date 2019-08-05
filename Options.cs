using CommandLine;

namespace repo_version
{
    class Options : OptionsBase
    {
        [Option('o', "output",
            Default = "semver",
            HelpText = "The output format. Should be one of [semver, json]")]
        public string Format { get; set; }
    }
}
