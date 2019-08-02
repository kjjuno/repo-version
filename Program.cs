using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Newtonsoft.Json;
using Version = System.Version;
using CommandLine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace repo_version
{
    class Configuration
    {
        public int Major { get; set; } 
        public int Minor { get; set; } 
        public List<BranchConfig> Branches { get; set; }
    }

    class BranchConfig
    {
        public string Regex { get; set; }
        public string Tag { get; set; }
    }

    class RepoVersion : IComparable
    {
        private string _tag;

        public string SemVer
        {
            get
            {
                if (!string.IsNullOrEmpty(PreReleaseTag))
                {
                    return $"{Major}.{Minor}.{Patch}.{Commits}-{PreReleaseTag}";
                }

                return $"{Major}.{Minor}.{Patch}.{Commits}";
            }
        }
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public int Commits { get; set; }
        public string PreReleaseTag
        {
            get => _tag ?? string.Empty;
            set => _tag = value;
        }

        public override string ToString()
        {
            return SemVer;
        }
        
        private static int ParseInt(string input)
        {
            var val = !string.IsNullOrEmpty(input) ? input : "0";

            return int.Parse(val);
        }

        public static bool TryParse(string input, out RepoVersion version)
        {
            version = null;
            var match = Regex.Match(input, @"(?<major>\d+)(?:\.(?<minor>\d+)(?:\.(?<patch>\d+)(?:\.(?<commits>\d+))?)?)?(?:-(?<tag>.+))?");

            if (!match.Success)
            {
                return false;
            }

            version = new RepoVersion();
            version.Major = ParseInt(match.Groups["major"].Value);
            version.Minor = ParseInt(match.Groups["minor"].Value);
            version.Patch = ParseInt(match.Groups["patch"].Value);
            version.Commits = ParseInt(match.Groups["commits"].Value);
            version.PreReleaseTag = match.Groups["tag"].Value ?? "";

            return true;
        }

        public int CompareTo(object obj)
        {
            var other = obj as RepoVersion;

            if (other == null)
            {
                return 1;
            }

            int major = this.Major.CompareTo(other.Major);

            if (major != 0)
            {
                return major;
            }

            int minor = this.Minor.CompareTo(other.Minor);

            if (minor != 0)
            {
                return minor;
            }

            int patch = this.Patch.CompareTo(other.Patch);

            if (patch != 0)
            {
                return patch;
            }

            int commits = this.Commits.CompareTo(other.Commits);

            if (commits != 0)
            {
                return commits;
            }

            return this.PreReleaseTag.CompareTo(other.PreReleaseTag);
        }
    }

    class Options
    {
        [Option('o', "output",
            Default = "semver",
            HelpText = "The output format. Should be one of [semver, json]")]
        public string Format { get; set; }

        [Value(0,
            MetaName = "path",
            Default = ".",
            HelpText = "Path to a git repository.")]
        public string Path { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var config = new Configuration();
            config.Major = 0;
            config.Major = 1;
            config.Branches = new List<BranchConfig>
            {
                new BranchConfig
                {
                    Regex = "^master$",
                    Tag = ""
                },
                new BranchConfig
                {
                    Regex = "^support[/-].*$",
                    Tag = ""
                },
                new BranchConfig
                {
                    Regex = ".+",
                    Tag = "{BranchName}"
                },
            };
            var configFile = "repo-version.json";
            if (File.Exists(configFile))
            {
                var json = File.ReadAllText(configFile);
                config = JsonConvert.DeserializeObject<Configuration>(json);
            }

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(options => RunOptionsAndReturnExitCode(options, config))
                .WithNotParsed<Options>((errors) => HandleParseErrors(errors));
        }


        private static void RunOptionsAndReturnExitCode(Options options, Configuration config)
        {
            var response = CalculateVersion(options.Path, config, options);

            if (string.Compare(options.Format, "json", StringComparison.OrdinalIgnoreCase) == 0)
            {
                Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
            }
            else
            {
                Console.WriteLine(response.SemVer);
            }

            Environment.ExitCode = 0;
        }

        private static void HandleParseErrors(IEnumerable<Error> errors)
        {
            Console.WriteLine();
            Environment.ExitCode = 1;
        }

        public static RepoVersion CalculateVersion(string path, Configuration config, Options options)
        {
            var curr = new DirectoryInfo(path);

            while (curr != null && !Directory.Exists(Path.Combine(curr.FullName, ".git")))
            {
                curr = curr.Parent;
            }

            if (curr == null)
            {
                Console.WriteLine("not a git repository");
                return null;
            }

            Repository repo = new Repository(curr.FullName);

            var status = repo.RetrieveStatus();

            var queryTags = from t in repo.Tags
                let commit = t.PeeledTarget
                where commit != null
                select new
                {
                    Commit = commit,
                    Tag = t
                };

            var lookup = queryTags.ToLookup(x => x.Commit.Id, x => x.Tag);

            var count = 0;

            var lastTag = new RepoVersion
            {
                Major = config.Major,
                Minor =  config.Minor,
            };

            foreach (var commit in repo.Commits)
            {
                var t = lookup[commit.Id];
                var tag = t.FirstOrDefault()?.FriendlyName.TrimStart('v', 'V');

                if (!string.IsNullOrEmpty(tag) && RepoVersion.TryParse(tag, out var v))
                {
                    lastTag = v;
                    break;
                }
                count++;
            }
            var major = lastTag.Major;
            var minor = lastTag.Minor;
            var patch = lastTag.Patch;
            var commits = lastTag.Commits;

            var preReleaseTag = "";

            if (count == 0 && status.IsDirty)
            {
                count++;
            }

            // Use the pre-release tag specified by the tag on the current commit
            if (count == 0 && !status.IsDirty)
            {
                preReleaseTag = lastTag.PreReleaseTag;
            }
            // if no tag exists at the current commit, calculate the pre-release tag
            else
            {
                preReleaseTag = CalculatePreReleaseTag(repo, config);
            }

            // If nothing bumped it from the initial version
            // set the version to 0.1.0
            if (major == 0 & minor == 0 && patch == 0)
            {
                minor = 1;
            }
            else if (count > 0)
            {
                commits += count;

                // Only increase the patch if there is no pre-release tag on the last git tag
                if (string.IsNullOrEmpty(lastTag.PreReleaseTag))
                {
                    patch++;
                    commits = count;
                }
            }

            var response = new RepoVersion
            {
                Major = Math.Max(major, config.Major),
                Minor = Math.Max(minor, config.Minor),
                Patch = patch,
                Commits = commits,
                PreReleaseTag = preReleaseTag
            };

            return response;
        }

        static RepoVersion ParseVersion(string input)
        {
            var str = input.TrimStart('v', 'V');

            if (!RepoVersion.TryParse(str, out var v))
            {
                return null;
            }

            return v;
        }

        private static string CalculatePreReleaseTag(Repository repo, Configuration config)
        {
            bool found = false;
            var preReleaseTag = "";
            var branch = repo.Head.FriendlyName;

            foreach (var branchConfig in config.Branches)
            {
                if (Regex.IsMatch(branch, branchConfig.Regex))
                {
                    preReleaseTag = branchConfig.Tag;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                preReleaseTag = "{BranchName}";
            }

            if (preReleaseTag.Contains("{BranchName}"))
            {
                var idx = branch.LastIndexOf('/');
                if (idx >= 0)
                {
                    branch = branch.Substring(idx + 1);
                }

                preReleaseTag = preReleaseTag
                    .Replace("{BranchName}", branch)
                    .Replace('_', '-');
                preReleaseTag = preReleaseTag
                    .Substring(0, Math.Min(30, preReleaseTag.Length))
                    .TrimEnd('-');
            }

            return preReleaseTag;
        }
    }
}
