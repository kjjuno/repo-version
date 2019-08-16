using System;

namespace repo_version.Commands
{
    class BumpMinorVersionCommand : ModifyConfigCommand
    {
        public BumpMinorVersionCommand() : base(create: false)
        { }

        protected override void TransformConfiguration(Options options, Configuration config)
        {
            config.Minor++;
        }

        protected override void OnSuccess(Configuration config, string path)
        {
            Console.WriteLine("Version bumpted to {0}.{1}", config.Major, config.Minor);
        }
    }
}
