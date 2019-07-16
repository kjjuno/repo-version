using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace repo_version
{
	class Program
	{
		static void Main(string[] args)
		{
			Repository r = new Repository(".");

			Console.WriteLine("Current Branch: {0}", r.Head.FriendlyName);

			foreach (var t in r.Tags)
			{
				Console.WriteLine("Last Version: {0}", t.FriendlyName);
				var filter = new CommitFilter();
				filter.ExcludeReachableFrom = t.CanonicalName;
				filter.IncludeReachableFrom = r.Head.Tip.Sha;

				var res = r.Commits.QueryBy(filter).Count();
				Console.WriteLine("Commits: {0}", res);
			}
		}
	}


}
