namespace Hack2022
{
    public static class Constants
    {
        public const int NumberOfBytesInMegabyte = 1048576;

        public const string EcpQuickToolDownloadsPath = @"C:\ecpQuickTool\Downloads";

        // Azure Paths
        public static class AzurePaths
        {
            public const string TestBuildsAzurePath = @"Testing\";
            public const string RiposteCoreAzurePath = @"Testing\RiposteCore\";
            public const string EssentialCPAzurePath = @"Testing\EssentialCP\";
        }

        // Exepected Directories
        public const string EssentialCPDirectoryName = "EssentialCP";
        public const string RiposteCoreDirectoryName = "RiposteCore";

        public static class Messages
        {
            public const string AzureConnectionSuccess = "Azure Connection is VALID.";
            public const string AzureConnectionError = "Azure Connection is INVALID.";

            public const string NoVersionsFound = "No Versions Found.";
            public const string NoSubVersionsFound = "No Sub-Versions Found for: ";
        }

        public static class RiposteCoreAzureFile
        {
            public const string FileNamePrefix = "Riposte_";
            public const string FileNameSuffix = "_Install-WS.zip";
            public const string FilePathPrefix = @"\PackageOutput\RMT\Riposte";
        }

        public static class EssentialCPAzureFile
        {
            public const string FileNamePrefix = "EssentialCP_";
            public const string FileNameSuffix = "_Install-WS.zip";
            public const string FilePathPrefix = @"\PackageOutput\RMT";
        }

        public static class DefaultInstallLocations
        {
            public const string RiposteCore = @"C:\Counters\bin\Riposte.exe";
            public const string EssentialCP = @"C:\Program Files (x86)\EssentialCP\EGDesktop-CP.exe";
        }
    }
}
