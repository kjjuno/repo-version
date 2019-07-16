using System;

namespace repo_version
{
    class Program
    {
        static void Main(string[] args)
        {
			LibGit2Sharp.Repository r = new LibGit2Sharp.Repository(".");
			foreach (var t in r.Tags)
			{
				Console.WriteLine(t.FriendlyName);
			}
        }
    }
}
