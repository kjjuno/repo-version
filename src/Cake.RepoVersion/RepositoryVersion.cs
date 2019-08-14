namespace Cake.RepoVersion
{
    /// <summary>
    /// The version information for a git repository.
    /// </summary>
	public class RepositoryVersion
    {
        /// <summary>
        /// The semantic version.
        /// </summary>
        public string SemVer { get; set; }

        /// <summary>
        /// The major version.
        /// </summary>
        public int Major { get; set; }

        /// <summary>
        /// The minor version.
        /// </summary>
        public int Minor { get; set; }

        /// <summary>
        /// The patch number.
        /// </summary>
        public int Patch { get; set; }

        /// <summary>
        /// The number of commits in this version.
        /// </summary>
        public int Commits { get; set; }

        /// <summary>
        /// Indicates if the repository has local changes.
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// The pre-release label. This will be null or empty if the version is not a pre-release.
        /// </summary>
        public string Label { get; set; }
    }
}
