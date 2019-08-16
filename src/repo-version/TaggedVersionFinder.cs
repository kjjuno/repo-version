using System.Linq;
using LibGit2Sharp;

namespace repo_version
{
    class TaggedVersionFinder
    {
        public RepoVersion GetLastTaggedVersion(Repository repo, out int commitsSinceTag)
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
    }
}
