using System;

namespace repo_version.Commands
{
    class BumpMajorVersionCommand : ModifyConfigCommand
    {
        public BumpMajorVersionCommand() : base(create: false)
        { }

        protected override void TransformConfiguration(Options options, Configuration config)
        {
            config.Major++;
            config.Minor = 0;
        }

        protected override void OnSuccess(Configuration config, string path)
        {
            Console.WriteLine("Version bumped to {0}.{1}", config.Major, config.Minor);
        }
    }
}
