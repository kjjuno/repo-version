using System;

namespace repo_version.Commands
{
    public class InitConfigCommand : ModifyConfigCommand
    {
        private readonly IVersionCalculator _calculator;

        public InitConfigCommand(IVersionCalculator calculator) : base(create: true)
        {
            _calculator = calculator;
        }

        protected override void TransformConfiguration(Options options, Configuration config)
        {
            var version = _calculator.CalculateVersion(options);

            config.Major = version.Major;
            config.Minor = version.Minor;

            Console.WriteLine("Please enter the major and minor versions for this repository");
            Console.Write("Major: ({0}) ", version.Major);
            var input = Console.ReadLine();

            if (!string.IsNullOrEmpty(input) && int.TryParse(input, out var major))
            {
                config.Major = major;
            }

            Console.Write("Minor: ({0}) ", version.Minor);
            input = Console.ReadLine();

            if (!string.IsNullOrEmpty(input) && int.TryParse(input, out var minor))
            {
                config.Minor = minor;
            }
        }

        protected override void OnSuccess(Configuration config, string path)
        {
            Console.WriteLine("created {0}", path);
        }
    }
}
