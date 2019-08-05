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
            var parser = new Parser(o =>
            {
                o.HelpWriter = null;
            });

            var result = parser.ParseArguments<Options, InitOptions, BumpMajorVersionOptions, BumpMinorVersionOptions>(args);

            if ((result is NotParsed<object>) && ((NotParsed<object>)result).Errors.Any(o => o.Tag == ErrorType.NoVerbSelectedError || o.Tag == ErrorType.BadVerbSelectedError))
            {
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed<Options>(options => RunOptionsAndReturnExitCode(options))
                    .WithNotParsed<Options>((errors) => HandleParseErrors(errors));
            }
            else
            {
                Parser.Default.ParseArguments<Options, InitOptions, BumpMajorVersionOptions, BumpMinorVersionOptions>(args)
                    .WithParsed<InitOptions>(options => Init(options))
                    .WithParsed<BumpMajorVersionOptions>(options => BumpMajorVersion(options))
                    .WithParsed<BumpMinorVersionOptions>(options => BumpMinorVersion(options))
                    .WithNotParsed((errors) => HandleParseErrors(errors));
            }
        }

        private static void ModifyConfig(string path, bool create, Action<Configuration> transform, Action<Configuration, string> success)
        {
            var gitFolder = FindGitFolder(path);

            if (gitFolder == null)
            {
                Console.WriteLine("not a git repository");
                return;
            }

            var config = Configuration.Load(gitFolder, create);

            if (config == null)
            {
                Console.WriteLine("No repo-version.json file. Please run repo-version init");
                return;
            }

            transform(config);

            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            path = Path.Combine(gitFolder, "repo-version.json");
            File.WriteAllText(path, json);

            success(config, path);
        }

        private static void Init(InitOptions options)
        {
            ModifyConfig(options.Path,
                create: true,
                transform: config =>
                {
                    Console.WriteLine("Please enter the major and minor versions for this repository");
                    Console.Write("Major: ({0}) ", config.Major);
                    var input = Console.ReadLine();

                    if (!string.IsNullOrEmpty(input) && int.TryParse(input, out var major))
                    {
                        config.Major = major;
                    }

                    Console.Write("Minor: ({0}) ", config.Minor);
                    input = Console.ReadLine();

                    if (!string.IsNullOrEmpty(input) && int.TryParse(input, out var minor))
                    {
                        config.Minor = minor;
                    }
                },
                success: (config, path) =>
                {
                    Console.WriteLine("created {0}", path);
                });
        }

        private static void BumpMajorVersion(BumpMajorVersionOptions options)
        {
            ModifyConfig(options.Path,
                create: false,
                transform: config =>
                {
                    config.Major++;
                    config.Minor = 0;
                },
                success: (config, path) =>
                {
                    Console.WriteLine("Version bumpted to {0}.{1}", config.Major, config.Minor);
                });
        }

        private static void BumpMinorVersion(BumpMinorVersionOptions options)
        {
            ModifyConfig(options.Path,
                create: false,
                transform: config =>
                {
                    config.Minor++;
                },
                success: (config, path) =>
                {
                    Console.WriteLine("Version bumpted to {0}.{1}", config.Major, config.Minor);
                });
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

        private static string FindGitFolder(string path)
        {
            var curr = new DirectoryInfo(path);

            while (curr != null && !Directory.Exists(Path.Combine(curr.FullName, ".git")))
            {
                curr = curr.Parent;
            }

            return curr?.FullName;
        }

        private static RepoVersion GetLastTaggedVersion(Repository repo, out int commitsSinceTag)
        {
            var queryTags = from t in repo.Tags
                let commit = t.PeeledTarget
                where commit != null
                select new
                {
                    Commit = commit,
                    Tag = t
                };

            var lookup = queryTags.ToLookup(x => x.Commit.Id, x => x.Tag);

            commitsSinceTag = 0;

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
                commitsSinceTag++;
            }

            return lastTag;
        }

        public static RepoVersion CalculateVersion(string path, Options options)
        {
            var gitFolder = FindGitFolder(path);

            if (gitFolder == null)
            {
                Console.WriteLine("not a git repository");
                return null;
            }

            var config = Configuration.Load(gitFolder, true);

            if (config == null)
            {
                return null;
            }

            var repo = new Repository(gitFolder);
            var lastTag = GetLastTaggedVersion(repo, out var commitsSinceTag);

            var major = lastTag?.Major ?? config.Major;
            var minor = lastTag?.Minor ?? config.Minor;
            var patch = lastTag?.Patch ?? 0;
            var commits = lastTag?.Commits ?? 0;

            var preReleaseTag = "";
            var status = repo.RetrieveStatus();

            // Use the pre-release tag specified by the tag on the current commit
            if (lastTag != null && commitsSinceTag == 0 && !status.IsDirty)
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
            else if (commitsSinceTag > 0)
            {
                commits += commitsSinceTag;

                // Only increase the patch if there is no pre-release tag on the last git tag
                if (lastTag?.PreReleaseTag == "")
                {
                    patch++;
                    commits = commitsSinceTag;
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
                PreReleaseTag = preReleaseTag,
                IsDirty = status.IsDirty
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

            if (config?.Branches != null)
            {
                foreach (var branchConfig in config.Branches)
                {
                    if (Regex.IsMatch(branch, branchConfig.Regex))
                    {
                        preReleaseTag = branchConfig.Tag;
                        found = true;
                        break;
                    }
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
                    .Replace("{BranchName}", branch);
            }

            preReleaseTag = preReleaseTag
                .Replace("(no branch)", "detached-head")
                .Replace('_', '-')
                .Replace("(", "")
                .Replace(")", "")
                .Replace(" ", "-");

            preReleaseTag = preReleaseTag
                .Substring(0, Math.Min(30, preReleaseTag.Length))
                .TrimEnd('-');

            return preReleaseTag;
        }
    }
}
