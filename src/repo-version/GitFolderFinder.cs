using System.IO;

namespace repo_version
{
    public class GitFolderFinder : IGitFolderFinder
    {
        public string FindGitFolder(string path)
        {
            var curr = new DirectoryInfo(path);

            while (curr != null && !Directory.Exists(Path.Combine(curr.FullName, ".git")))
            {
                curr = curr.Parent;
            }

            return curr?.FullName;
        }
    }
}
