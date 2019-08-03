using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CommandLine;
using LibGit2Sharp;
using Newtonsoft.Json;
using System.Linq;

namespace repo_version
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(options => RunOptionsAndReturnExitCode(options))
                .WithNotParsed<Options>((errors) => HandleParseErrors(errors));
        }

        private static Configuration LoadConfiguration(string path)
        {
            var config = new Configuration();
            var defaultConfig = new Configuration();
            defaultConfig.Major = 0;
            defaultConfig.Minor = 1;
            defaultConfig.Branches = new List<BranchConfig>
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
            var configFile = Path.Combine(path, "repo-version.json");
            if (File.Exists(configFile))
            {
                try
                {
                    var json = File.ReadAllText(configFile);
                    config = JsonConvert.DeserializeObject<Configuration>(json);

                    if (config.Branches == null)
                    {
                        config.Branches = defaultConfig.Branches;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to load repo-version.json. {0}", e.Message);
                    return null;
                }
            }

            return config;
        }

        private static void RunOptionsAndReturnExitCode(Options options)
        {
            var response = CalculateVersion(options.Path, options);

            if (response == null)
            {
                Environment.ExitCode = 1;
                return;
            }

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

        public static RepoVersion CalculateVersion(string path, Options options)
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

            var config = LoadConfiguration(curr.FullName);

            if (config == null)
            {
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

            RepoVersion lastTag = null;

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
            var major = lastTag?.Major ?? config.Major;
            var minor = lastTag?.Minor ?? config.Minor;
            var patch = lastTag?.Patch ?? 0;
            var commits = lastTag?.Commits ?? 0;

            var preReleaseTag = "";

            // If there are no useable tags or we are on a tagged commit, and there are uncommitted changes
            // increment the count by 1
            if (status.IsDirty)
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
                if (lastTag?.PreReleaseTag == "")
                {
                    patch++;
                    commits = count;
                }
            }

            if (config.Major > major)
            {
                major = config.Major;
                minor = config.Minor;
                patch = 0;
            }
            else if (config.Minor > minor)
            {
                minor = config.Minor;
                patch = 0;
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
