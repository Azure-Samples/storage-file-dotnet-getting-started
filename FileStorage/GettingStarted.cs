//----------------------------------------------------------------------------------
// Microsoft Azure Storage Team
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//----------------------------------------------------------------------------------

using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Sas;
using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// Create share, upload file, download file, list files and folders, copy file, abort copy file, write range, list ranges.
namespace FileStorage
{
    public class GettingStarted
    {
        /// <summary>
        /// Test some of the file storage operations.
        /// </summary>
        public async Task RunFileStorageOperationsAsync()
        {
            // These are used in the finally block to clean up the objects created during the demo.
            ShareClient shareClient = null;
            ShareFileClient shareFileClient = null;
            ShareDirectoryClient fileDirectory = null;
            
            BlobClient targetBlob = null;
            BlobContainerClient blobContainer = null;

            string destFile = null;
            string downloadFolder = null;
            // Name to be used for the file when downloading it so you can inspect it locally
            string downloadFile = null;
            try
            {
                //***** Setup *****//
                Console.WriteLine("Getting reference to the storage account.");

                // How to create a storage connection string - http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
                string storageConnectionString = ConfigurationManager.AppSettings.Get("StorageConnectionString");
                string storageAccountName = ConfigurationManager.AppSettings.Get("StorageAccountName");
                string storageAccountKey = ConfigurationManager.AppSettings.Get("StorageAccountKey");

                Console.WriteLine("Instantiating file client.");

                // Create a share client for interacting with the file service.
                var shareServiceClient = new ShareServiceClient(storageConnectionString);

                // Create the share name -- use a guid in the name so it's unique.
                // This will also be used as the container name for blob storage when copying the file to blob storage.
                string shareName = "demotest-" + System.Guid.NewGuid().ToString();

                // Name of folder to put the files in 
                string sourceFolder = "testfolder";

                // Name of file to upload and download 
                string testFile = "HelloWorld.png";

                // Folder where the HelloWorld.png file resides 
                string localFolder = @".\";

                // It won't let you download in the same folder as the exe file, 
                //   so use a temporary folder with the same name as the share.
                downloadFolder = Path.Combine(Path.GetTempPath(), shareName);

                //***** Create a file share *****//

                // Create the share if it doesn't already exist.
                Console.WriteLine("Creating share with name {0}", shareName);
                shareClient = shareServiceClient.GetShareClient(shareName);
                try
                {
                    await shareClient.CreateIfNotExistsAsync();
                    Console.WriteLine("    Share created successfully.");
                }
                catch (RequestFailedException exRequest)
                {
                    Common.WriteException(exRequest);
                    Console.WriteLine("Please make sure your storage account has storage file endpoint enabled and specified correctly in the app.config - then restart the sample.");
                    Console.WriteLine("Press any key to exit");
                    Console.ReadLine();
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("    Exception thrown creating share.");
                    Common.WriteException(ex);
                    throw;
                }

                //***** Create a directory on the file share *****//

                // Create a directory on the share.
                Console.WriteLine("Creating directory named {0}", sourceFolder);

                ShareDirectoryClient rootDirectory = shareClient.GetRootDirectoryClient();

                // If the source folder is null, then use the root folder.
                // If the source folder is specified, then get a reference to it.
                if (string.IsNullOrWhiteSpace(sourceFolder))
                {
                    // There is no folder specified, so return a reference to the root directory.
                    fileDirectory = rootDirectory;
                    Console.WriteLine("    Using root directory.");
                }
                else
                {
                    // There was a folder specified, so return a reference to that folder.
                    fileDirectory = rootDirectory.GetSubdirectoryClient(sourceFolder);

                    await fileDirectory.CreateIfNotExistsAsync();
                    Console.WriteLine("    Directory created successfully.");
                }

                //***** Upload a file to the file share *****//

                // Get a file client.
                shareFileClient = fileDirectory.GetFileClient(testFile);

                // Upload a file to the share.
                Console.WriteLine("Uploading file {0} to share", testFile);

                // Set up the name and path of the local file.
                string sourceFile = Path.Combine(localFolder, testFile);
                if (File.Exists(sourceFile))
                {
                    using (FileStream stream = File.OpenRead(sourceFile))
                    {
                        // Upload from the local file to the file share in azure.
                        await shareFileClient.CreateAsync(stream.Length);
                        await shareFileClient.UploadAsync(stream);
                    }
                    Console.WriteLine("    Successfully uploaded file to share.");
                }
                else
                {
                    Console.WriteLine("File not found, so not uploaded.");
                }

                //***** Get list of all files/directories on the file share*****//

                // List all files/directories under the root directory.
                Console.WriteLine("Getting list of all files/directories under the root directory of the share.");

                var fileList = rootDirectory.GetFilesAndDirectoriesAsync();

                // Print all files/directories listed above.
                await foreach (ShareFileItem listItem in fileList)
                {
                    // listItem type will be ShareClient or ShareDirectoryClient.
                    Console.WriteLine("    - {0} (type: {1})", listItem.Name, listItem.GetType());
                }

                Console.WriteLine("Getting list of all files/directories in the file directory on the share.");

                // Now get the list of all files/directories in your directory.
                // Ordinarily, you'd write something recursive to do this for all directories and subdirectories.

                fileList = fileDirectory.GetFilesAndDirectoriesAsync();

                // Print all files/directories in the folder.
                await foreach (ShareFileItem listItem in fileList)
                {
                    // listItem type will be a file or directory
                    Console.WriteLine("    - {0} (IsDirectory: {1})", listItem.Name, listItem.IsDirectory);
                }

                //***** Download a file from the file share *****//

                // Download the file to the downloadFolder in the temp directory.
                // Check and if the directory doesn't exist (which it shouldn't), create it.
                Console.WriteLine("Downloading file from share to local temp folder {0}.", downloadFolder);
                if (!Directory.Exists(downloadFolder))
                {
                    Directory.CreateDirectory(downloadFolder);
                }

                // Download the file.
                ShareFileDownloadInfo download = await shareFileClient.DownloadAsync();

                downloadFile = Path.Combine(downloadFolder, testFile);
                using (FileStream stream = File.OpenWrite(downloadFile))
                {
                    await download.Content.CopyToAsync(stream);
                }

                Console.WriteLine("    Successfully downloaded file from share to local temp folder.");

                //***** Copy a file from the file share to blob storage, then abort the copy *****//

                // Copies can sometimes complete before there's a chance to abort. 
                // If that happens with the file you're testing with, try copying the file 
                //   to a storage account in a different region. If it still finishes too fast,
                //   try using a bigger file and copying it to a different region. That will almost always 
                //   take long enough to give you time to abort the copy. 
                // If you want to change the file you're testing the Copy with without changing the value for the 
                //   rest of the sample code, upload the file to the share, then assign the name of the file 
                //   to the testFile variable right here before calling GetFileClient. 
                //   Then it will use the new file for the copy and abort but the rest of the code
                //   will still use the original file.
                ShareFileClient shareFileCopy = fileDirectory.GetFileClient(testFile);

                // Upload a file to the share.
                Console.WriteLine("Uploading file {0} to share", testFile);

                // Set up the name and path of the local file.
                string sourceFileCopy = Path.Combine(localFolder, testFile);
                using (FileStream stream = File.OpenRead(sourceFile))
                {
                    // Upload from the local file to the file share in azure.
                    await shareFileCopy.CreateAsync(stream.Length);
                    await shareFileCopy.UploadAsync(stream);
                }
                Console.WriteLine("    Successfully uploaded file to share.");

                // Copy the file to blob storage.
                Console.WriteLine("Copying file to blob storage. Container name = {0}", shareName);

                // First get a blob service client. 
                BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnectionString);

                // Get a blob container client and create it if it doesn't already exist.
                blobContainer = blobServiceClient.GetBlobContainerClient(shareName);
                await blobContainer.CreateIfNotExistsAsync();

                // Get a blob client to the target blob.
                targetBlob = blobContainer.GetBlobClient(testFile);

                string copyId = string.Empty;

                // Get a share file client to be copied.
                shareFileClient = fileDirectory.GetFileClient(testFile);

                // Create a SAS for the file that's valid for 24 hours.
                // Note that when you are copying a file to a blob, or a blob to a file, you must use a SAS
                // to authenticate access to the source object, even if you are copying within the same
                // storage account.
                AccountSasBuilder sas = new AccountSasBuilder
                {
                    // Allow access to Files
                    Services = AccountSasServices.Files,

                    // Allow access to the service level APIs
                    ResourceTypes = AccountSasResourceTypes.All,

                    // Access expires in 1 day!
                    ExpiresOn = DateTime.UtcNow.AddDays(1)
                };
                sas.SetPermissions(AccountSasPermissions.Read);

                StorageSharedKeyCredential credential = new StorageSharedKeyCredential(storageAccountName, storageAccountKey);

                // Build a SAS URI
                UriBuilder sasUri = new UriBuilder(shareFileClient.Uri)
                {
                    Query = sas.ToSasQueryParameters(credential).ToString()
                };
                // Start the copy of the file to the blob.
                CopyFromUriOperation operation = await targetBlob.StartCopyFromUriAsync(sasUri.Uri);
                copyId = operation.Id;

                Console.WriteLine("   File copy started successfully. copyID = {0}", copyId);

                // Now clean up after yourself.
                Console.WriteLine("Deleting the files from the file share.");

                // Delete the files because cloudFile is a different file in the range sample.
                shareFileClient = fileDirectory.GetFileClient(testFile);
                await shareFileClient.DeleteIfExistsAsync();

                Console.WriteLine("Setting up files to test WriteRange and ListRanges.");

                //***** Write 2 ranges to a file, then list the ranges *****//

                // This is the code for trying out writing data to a range in a file, 
                //   and then listing those ranges.
                // Get a reference to a file and write a range of data to it      .
                // Then write another range to it.
                // Then list the ranges.

                // Start at the very beginning of the file.
                long startOffset = 0;

                // Set the destination file name -- this is the file on the file share that you're writing to.
                destFile = "rangeops.txt";
                shareFileClient = fileDirectory.GetFileClient(destFile);

                // Create a string with 512 a's in it. This will be used to write the range.
                int testStreamLen = 512;
                string textToStream = string.Empty;
                textToStream = textToStream.PadRight(testStreamLen, 'a');

                using (MemoryStream ms = new MemoryStream(Encoding.Default.GetBytes(textToStream)))
                {
                    // Max size of the output file; have to specify this when you create the file
                    // I picked this number arbitrarily.
                    long maxFileSize = 65536;

                    Console.WriteLine("Write first range.");

                    // Set the stream back to the beginning, in case it's been read at all.
                    ms.Position = 0;

                    // If the file doesn't exist, create it.
                    // The maximum file size is passed in. It has to be big enough to hold
                    //   all the data you're going to write, so don't set it to 256k and try to write two 256-k blocks to it. 
                    if (!shareFileClient.Exists())
                    {
                        Console.WriteLine("File doesn't exist, create empty file to write ranges to.");

                        // Create a file with a maximum file size of 64k. 
                        await shareFileClient.CreateAsync(maxFileSize);
                        Console.WriteLine("    Empty file created successfully.");
                    }

                    // Write the stream to the file starting at startOffset for the length of the stream.
                    Console.WriteLine("Writing range to file.");
                    HttpRange range = new HttpRange(startOffset, textToStream.Length);
                    await shareFileClient.UploadRangeAsync(range, ms);

                    // Download the file to your temp directory so you can inspect it locally.
                    downloadFile = Path.Combine(downloadFolder, "__testrange.txt");
                    Console.WriteLine("Downloading file to examine.");
                    download = await shareFileClient.DownloadAsync();
                    using (FileStream stream = File.OpenWrite(downloadFile))
                    {
                        await download.Content.CopyToAsync(stream);
                    }

                    Console.WriteLine("    Successfully downloaded file with ranges in it to examine.");
                }

                // Now add the second range, but don't make it adjacent to the first one, or it will show only 
                //   one range, with the two combined. Put it like 1000 spaces away. When you get the range back, it will 
                //   start at the position at the 512-multiple border prior or equal to the beginning of the data written,
                //   and it will end at the 512-multliple border after the actual end of the data.
                //For example, if you write to 2000-3000, the range will be the 512-multiple prior to 2000, which is 
                //   position 1536, or offset 1535 (because it's 0-based).
                //   And the right offset of the range will be the 512-multiple after 3000, which is position 3072,
                //   or offset 3071 (because it's 0-based).
                Console.WriteLine("Getting ready to write second range to file.");

                startOffset += testStreamLen + 1000; //randomly selected number

                // Create a string with 512 b's in it. This will be used to write the range.
                textToStream = string.Empty;
                textToStream = textToStream.PadRight(testStreamLen, 'b');

                using (MemoryStream ms = new MemoryStream(Encoding.Default.GetBytes(textToStream)))
                {

                    ms.Position = 0;

                    // Write the stream to the file starting at startOffset for the length of the stream.
                    Console.WriteLine("Write second range to file.");
                    HttpRange range = new HttpRange(startOffset, textToStream.Length);
                    await shareFileClient.UploadRangeAsync(range, ms);
                    Console.WriteLine("    Successful writing second range to file.");

                    // Download the file to your temp directory so you can examine it.
                    downloadFile = Path.Combine(downloadFolder, "__testrange2.txt");
                    Console.WriteLine("Downloading file with two ranges in it to examine.");

                    download = await shareFileClient.DownloadAsync();

                    using (FileStream stream = File.OpenWrite(downloadFile))
                    {
                        await download.Content.CopyToAsync(stream);
                    }
                    Console.WriteLine("    Successfully downloaded file to examine.");
                }

                // Query and view the list of ranges.
                Console.WriteLine("Call to get the list of ranges.");
                var listOfRanges = await shareFileClient.GetRangeListAsync(new HttpRange());
                

                Console.WriteLine("    Successfully retrieved list of ranges.");
                foreach (HttpRange range in listOfRanges.Value.Ranges)
                {
                    Console.WriteLine("    --> filerange startOffset = {0}, endOffset = {1}", range.Offset, range.Offset + range.Length);
                }
                
                //***** Clean up *****//
             
            }
            catch (Exception ex)
            {
                Console.WriteLine("    Exception thrown. Message = {0}{1}    Strack Trace = {2}", ex.Message, Environment.NewLine, ex.StackTrace);
            }
            finally
            {
                //Clean up after you're done.

                Console.WriteLine("Removing all files, folders, shares, blobs, and containers created in this demo.");

                // ****NOTE: You can just delete the file share, and everything will be removed. 
                // This samples deletes everything off of the file share first for the purpose of
                //   showing you how to delete specific files and directories.


                // Delete the file with the ranges in it.
                destFile = "rangeops.txt";
                shareFileClient = fileDirectory.GetFileClient(destFile);
                await shareFileClient.DeleteIfExistsAsync();

                Console.WriteLine("Deleting the directory on the file share.");

                // Delete the directory.
                bool success = await fileDirectory.DeleteIfExistsAsync();
                if (success)
                {
                    Console.WriteLine("    Directory on the file share deleted successfully.");
                }
                else
                {
                    Console.WriteLine("    Directory on the file share NOT deleted successfully; may not exist.");
                }

                Console.WriteLine("Deleting the file share.");

                // Delete the share.
                await shareClient.DeleteAsync();
                Console.WriteLine("    Deleted the file share successfully.");

                Console.WriteLine("Deleting the temporary download directory and the file in it.");

                // Delete the download folder and its contents.
                Directory.Delete(downloadFolder, true);
                Console.WriteLine("    Successfully deleted the temporary download directory.");

                Console.WriteLine("Deleting the container and blob used in the Copy/Abort test.");
                await targetBlob.DeleteIfExistsAsync();
                await blobContainer.DeleteIfExistsAsync();
                Console.WriteLine("    Successfully deleted the blob and its container.");

            }

        }

    }
}
