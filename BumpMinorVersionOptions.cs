using CommandLine;

namespace repo_version
{
    [Verb("minor", HelpText = "Bumps the version to the next minor version")]
    class BumpMinorVersionOptions : OptionsBase
    { } 
}
