using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hack2022.AzureConnect
{
    // View Link Below for basics:
    // https://docs.microsoft.com/en-us/dotnet/api/overview/azure/storage.files.shares-readme
    public static class AzureFileShareMethods
    {
        public static List<ShareFileItem> GetDirectories(this ShareClient fileShareClient, 
                                                         List<string> directoryNames = null, 
                                                         string startingDirectoryPath = null,
                                                         bool loopOnSubDirectories = false)
        {
            var results = new List<ShareFileItem>();
            bool getSpecificDirectories = directoryNames != null && directoryNames.Any(n => n.Length > 0);

            // Track the remaining directories to process, starting from the root.
            var remainingDirsToProcess = new Queue<ShareDirectoryClient>();

            // Start at specified Path or start at File Share Root Dir, by adding it to processing list.
            if (startingDirectoryPath == null)
            {
                remainingDirsToProcess.Enqueue(fileShareClient.GetRootDirectoryClient());
            }
            else
            {
                remainingDirsToProcess.Enqueue(fileShareClient.GetDirectoryClient(startingDirectoryPath));
            }

            while (remainingDirsToProcess.Count > 0)
            {
                // Get current directory and remove from remaining directories to process.
                ShareDirectoryClient dir = remainingDirsToProcess.Dequeue();

                foreach (ShareFileItem item in dir.GetFilesAndDirectories()) // Get and process all child nodes.
                {
                    if (item.IsDirectory)
                    {
                        // Specified Directory Name is found.
                        if (getSpecificDirectories)
                        {
                            if (directoryNames.Contains(item.Name)) results.Add(item);
                        }
                        else // Add all directories found to list.
                        {
                            results.Add(item);
                        }

                        // Process sub-dirs.
                        if (loopOnSubDirectories) remainingDirsToProcess.Enqueue(dir.GetSubdirectoryClient(item.Name));
                    }
                }
            }

            return results;
        }

        public static long DownloadFile(this ShareClient shareClient, string directoryPath, string fileName, string localDownloadPath)
        {
            // Get a reference to the file
            ShareDirectoryClient directory = shareClient.GetDirectoryClient(directoryPath);
            ShareFileClient fileClient = directory.GetFileClient(fileName);

            // Download the file.
            ShareFileDownloadInfo downloadInfo = fileClient.Download();

            // Get File Size.
            long fileSize = downloadInfo.ContentLength;

            string localFilePath = Helper.CombinePath(localDownloadPath, fileName);

            using (FileStream stream = File.OpenWrite(localFilePath))
            {
                downloadInfo.Content.CopyTo(stream);
            }

            return fileSize;
        }

        // TODO: Enhance and make it work with Progress Bar.
        //public async static void DownloadFileAsync(this ShareClient shareClient, string directoryPath, string fileName, 
        //                                           string localDownloadPath, IProgress<int> progress)
        //{
        //    // Get a reference to the file
        //    ShareDirectoryClient directory = shareClient.GetDirectoryClient(directoryPath);
        //    ShareFileClient fileClient = directory.GetFileClient(fileName);

        //    // Download the file asynchronously.
        //    ShareFileDownloadInfo downloadInfo = await fileClient.DownloadAsync();

        //    // Get File Size.
        //    long fileSize = downloadInfo.ContentLength;

        //    string localFilePath = Helper.CombinePath(localDownloadPath, fileName);

        //    using (FileStream stream = File.OpenWrite(localFilePath))
        //    {
        //        await downloadInfo.Content.CopyToAsync(stream);
        //    }

        //    while (downloadInfo.Details.CopyStatus != CopyStatus.Success)
        //    {
        //        long downloadedBytes = string.IsNullOrWhiteSpace(downloadInfo.Details.CopyProgress) ? 0 : long.Parse(downloadInfo.Details.CopyProgress);
        //        progress.Report((int)(downloadedBytes / fileSize));
        //    }
        //}
    }
}
