using System;
using System.Linq;
using LibGit2Sharp;
using Version = System.Version;

namespace repo_version
{
	class Program
	{
		static Version ParseVersion(string input)
		{
			var str = input.TrimStart('v', 'V');

			if (!Version.TryParse(str, out var v))
			{
				return null;
			}

			return v;
		}

		static void Main(string[] args)
		{
			Repository r = new Repository("/home/kjjuno/git/claims/services/external-api");

			Console.WriteLine("Current Branch: {0}", r.Head.FriendlyName);

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

			if (latest != null)
			{
				Console.WriteLine("Last Version: {0}", latest.Tag.FriendlyName);
				var filter = new CommitFilter();
				filter.ExcludeReachableFrom = latest.Tag.CanonicalName;
				filter.IncludeReachableFrom = r.Head.Tip.Sha;

				var commits = r.Commits.QueryBy(filter).Count();
				Console.WriteLine("Commits: {0}", commits);
				var version = latest.Version;

				if (commits > 0)
				{
					version = new Version(version.Major, version.Minor, version.Build + 1);
				}

				Console.WriteLine("Version: {0}", version);
			}
		}
	}
}
