using LibGit2Sharp;

namespace repo_version
{
    public interface ITaggedVersionFinder
    {
        RepoVersion GetLastTaggedVersion(IRepository repo, out int commitsSinceTag);
    }
}
