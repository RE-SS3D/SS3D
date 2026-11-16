namespace SS3D.Utils
{
    /// <summary>
    /// The GitHub repository players' builds talk to: release checks and the bug report link.
    /// <see cref="Slug"/> is mirrored in the updater scripts under <c>Builds/</c> and <c>Tools/updater/</c>.
    /// Change all of them with <c>python3 Tools/updater/set_project_repo.py owner/name</c>, never by hand;
    /// <c>Tools/updater/test_project_repo.py</c> fails when they disagree.
    /// </summary>
    public static class ProjectRepository
    {
        /// <summary>Owner and name, cased exactly as GitHub returns them in release download URLs.</summary>
        public const string Slug = "henkhooft/SS3D";

        public const string Url = "https://github.com/" + Slug;

        public const string ApiUrl = "https://api.github.com/repos/" + Slug;
    }
}
