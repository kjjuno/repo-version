using System;
using Newtonsoft.Json;

namespace repo_version.Commands
{
    public class DisplayVersionCommand : ICommand
    {
        private readonly IVersionCalculator _calculator;

        public DisplayVersionCommand(IVersionCalculator calculator)
        {
            _calculator = calculator;
        }

        public int Execute(Options options)
        {
            var version = _calculator.CalculateVersion(options);

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
