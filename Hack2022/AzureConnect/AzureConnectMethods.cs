using Azure.Storage.Files.Shares;

namespace Hack2022.AzureConnect
{
    public class AzureConnectMethods
    {
        public static ShareClient ConnectToFileShare(AzureFileShareSettings fileShareSettings)
        {
            try 
            {
                return new ShareClient(fileShareSettings.ConnectionString, fileShareSettings.FileShareName);
            }
            catch
            {
                return null;
            }

        }
    }
}
