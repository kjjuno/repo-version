using System;
using Newtonsoft.Json;

namespace repo_version.Commands
{
    class DisplayVersionCommand : ICommand
    {
        public int Execute(Options options)
        {
            var calculator = new VersionCalculator();
            var version = calculator.CalculateVersion(options);

            if (version == null)
            {
                return 1;
            }

            if (string.Compare(options.Format, "json", StringComparison.OrdinalIgnoreCase) == 0)
            {
                Console.WriteLine(JsonConvert.SerializeObject(version, Formatting.Indented));
            }
            else
            {
                Console.WriteLine(version.SemVer);
            }

            return 1;
        }
    }
}
