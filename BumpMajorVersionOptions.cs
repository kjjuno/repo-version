using CommandLine;

namespace repo_version
{
    [Verb("major", HelpText = "Bumps the version to the next major version")]
    class BumpMajorVersionOptions : OptionsBase
    { } 
}
