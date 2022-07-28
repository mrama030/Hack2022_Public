using Azure.Storage.Files.Shares.Models;

namespace Hack2022
{
    public class VersionInfo
    {
        public string Version;
        public string VersionFullPath;
        public ShareFileItem VersionFileShareFolder;

        public override string ToString()
        {
            return Version;
        }
    }
}
