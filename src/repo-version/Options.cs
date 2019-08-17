using System;
using System.IO;
using System.Text;

namespace repo_version
{
    public class Options
    {
        public string Path { get; private set; }
        public string Format { get; private set; }
        public bool ShowHelp { get; private set; }
        public string Verb { get; private set; }
        public bool ShowAssemblyVersion { get; private set; }
        public string Label { get; private set; }

        public string PrintHelp()
        {
            StringBuilder help = new StringBuilder();

            help.AppendLine("typical usage:");
            help.AppendLine("repo-version [--version] [-o | --output <format>] <path>");
            help.AppendLine();
            help.AppendLine("-o, --output             The output format. Legal values are [text, json]. The default is text.");
            help.AppendLine("-l, --label              Overrides the current label. If not provided it will be calculated.");
            help.AppendLine("-v, --version            Displays the version of repo-version");
            help.AppendLine("path                     The path to a git repository. The default is the current directory.");
            help.AppendLine();
            help.AppendLine("manipulate config file:");
            help.AppendLine("repo-version <command> <path>");
            help.AppendLine();
            help.AppendLine("<command> must be one of the following");
            help.AppendLine("init                enitializes a repository with a repo-version.json file.");
            help.AppendLine("major               Bumps the version in repo-version.json to the next major version.");
            help.AppendLine("minor               Bumps the version in repo-version.json to the next minor version.");
            help.AppendLine("tag                 tags the repository with the current version");

            return help.ToString();
        }

        public static Options Parse(string[] args)
        {
            var options = new Options();
            options.Path = ".";
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                switch (arg)
                {
                    case "-o":
                    case "--output":
                        options.Format = GetStringArg(++i, args);
                        break;
                    case "-l":
                    case "--label":
                        options.Label = GetStringArg(++i, args);
                        if (options.Label == null)
                        {
                            options.ShowHelp = true;
                            Console.Error.WriteLine("Expected a label to be specified.");
                        }
                        break;
                    case "-v":
                    case "--version":
                        options.ShowAssemblyVersion = true;
                        break;
                    case "-h":
                    case "--help":
                        options.ShowHelp = true;
                        break;
                    case "init":
                    case "major":
                    case "minor":
                    case "tag":
                        if (i == 0)
                        {
                            options.Verb = arg;
                        }
                        else
                        {
                            options.ShowHelp = true;
                            Console.Error.WriteLine($"unexpected command {arg}. Should be the first argument");
                        }
                        break;
                    default:
                        if (!arg.StartsWith("-") && i == args.Length - 1)
                        {
                            if (Directory.Exists(arg))
                            {
                                options.Path = arg;
                            }
                            else
                            {
                                Console.Error.WriteLine("Unknown argument {0}", arg);
                                Console.Error.WriteLine("Expected this to be a path, but that path does not exist");
                                options.ShowHelp = true;
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine("Unknown argument {0}", arg);
                            options.ShowHelp = true;
                        }
                        break;
                }
            }

            return options;
        }

        private static string GetStringArg(int idx, string[] args)
        {
            if (idx < args.Length)
            {
                return args[idx];
            }
            return null;
        }
    }
}
