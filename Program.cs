using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Newtonsoft.Json;
using Version = System.Version;
using CommandLine;
using System.Collections.Generic;

namespace repo_version
{
    class RepoVersion
    {
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
        public string PreReleaseTag { get; set; }

        public override string ToString()
        {
            return SemVer;
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
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(options => RunOptionsAndReturnExitCode(options))
                .WithNotParsed<Options>((errors) => HandleParseErrors(errors));
        }

        private static void RunOptionsAndReturnExitCode(Options options)
        {
            var response = CalculateVersion(options.Path);

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

        public static RepoVersion CalculateVersion(string path)
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

            Repository r = new Repository(curr.FullName);

            var query = from t in r.Tags
                let v = ParseVersion(t.FriendlyName)
                where v != null
                orderby v descending
                select new
                {
                    Tag = t,
                    Version = v
                };

            var latest = query.FirstOrDefault();

            var q = from t in r.Tags
                let commit = t.PeeledTarget
                where commit != null
                select new
                {
                    Commit = commit,
                    Tag = t
                };

            var lookup = q.ToLookup(x => x.Commit.Id, x => x.Tag);

            var count = 0;
            var version = new Version("0.0.0");
            foreach (var commit in r.Commits)
            {
                var t = lookup[commit.Id];
                var tag = t.FirstOrDefault()?.FriendlyName.TrimStart('v', 'V');

                if (!string.IsNullOrEmpty(tag) && Version.TryParse(tag, out var v))
                {
                    version = v;
                    break;
                }
                count++;
            }
            var major = version.Major;
            var minor = version.Minor;
            var patch = version.Build;
            var commits = version.Revision;
            var preReleaseTag = CalculatePreReleaseTag(r);

            // If nothing bumped it from the initial version
            // set the version to 0.1.0
            if (major == 0 & minor == 0 && patch == 0)
            {
                minor = 1;
            }
            else if (count > 0)
            {
                patch++;
                commits = count;
            }

            var response = new RepoVersion
            {
                Major = major,
                Minor = minor,
                Patch = patch,
                Commits = commits,
                PreReleaseTag = preReleaseTag
            };

            return response;
        }

        static Version ParseVersion(string input)
        {
            var str = input.TrimStart('v', 'V');

            if (!Version.TryParse(str, out var v))
            {
                return null;
            }

            return v;
        }

        private static string CalculatePreReleaseTag(Repository r)
        {
            var preReleaseTag = "";
            var branch = r.Head.FriendlyName;
            if (branch != "master")
            {
                var idx = branch.LastIndexOf('/');
                if (idx >= 0)
                {
                    preReleaseTag = branch.Substring(idx + 1);
                }
                else
                {
                    preReleaseTag = branch;
                }
                preReleaseTag = preReleaseTag.Replace('_', '-').Substring(0, Math.Min(30, preReleaseTag.Length));
            }

            return preReleaseTag;
        }
    }
}
