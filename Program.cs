using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Newtonsoft.Json;
using Version = System.Version;

namespace repo_version
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = ".";
            if (args.Length > 0)
            {
                path = args[0];
            }

            var response = CalculateVersion(path);

            Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
        }

        public static object CalculateVersion(string path)
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
            }


            var response = new
            {
                SemVer = SemVer(major, minor, patch, preReleaseTag, count),
                Major = major,
                Minor = minor,
                Patch = patch,
                Commits = count,
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

        private static string SemVer(int major, int minor, int patch, string preReleaseTag, int commits)
        {
            if (!string.IsNullOrEmpty(preReleaseTag))
            {
                return $"{major}.{minor}.{patch}.{commits}-{preReleaseTag}";
            }

            return $"{major}.{minor}.{patch}.{commits}";
        }

        private static string CalculatePreReleaseTag(Repository r)
        {
            var preReleaseTag = "";
            var branch = r.Head.FriendlyName;
            if (branch != "master")
            {
                var idx = branch.LastIndexOf('/');
                if (idx > 0)
                {
                    preReleaseTag = branch.Substring(idx + 1);
                }
                preReleaseTag = preReleaseTag.Replace('_', '-').Substring(0, Math.Min(30, preReleaseTag.Length));
            }

            return preReleaseTag;
        }


    }
}
