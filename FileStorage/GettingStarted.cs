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

using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using System;
using System.Collections.Generic;
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
            CloudFile cloudFile = null;
            CloudFileDirectory fileDirectory = null;
            string destFile = null;

            CloudBlob targetBlob = null;
            CloudFileShare cloudFileShare = null;
            string downloadFolder = null;
            CloudBlobContainer cloudBlobContainer = null;

            try
            {
                //***** Setup *****//
                Console.WriteLine("Getting reference to the storage account.");

                // Retrieve storage account information from connection string
                // How to create a storage connection string - http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
                CloudStorageAccount storageAccount = Common.CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

                Console.WriteLine("Instantiating file client.");

                // Create a file client for interacting with the file service.
                CloudFileClient cloudFileClient = storageAccount.CreateCloudFileClient();

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
                cloudFileShare = cloudFileClient.GetShareReference(shareName);
                try
                {
                    await cloudFileShare.CreateIfNotExistsAsync();
                    Console.WriteLine("    Share created successfully.");
                }
                catch (StorageException exStorage)
                {
                    Common.WriteException(exStorage);
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

                // First, get a reference to the root directory, because that's where you're going to put the new directory.
                CloudFileDirectory rootDirectory = cloudFileShare.GetRootDirectoryReference();

                // Set a reference to the file directory.
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
                    fileDirectory = rootDirectory.GetDirectoryReference(sourceFolder);

                    await fileDirectory.CreateIfNotExistsAsync();
                    Console.WriteLine("    Directory created successfully.");
                }

                //***** Upload a file to the file share *****//

                // Set a reference to the file.
                cloudFile = fileDirectory.GetFileReference(testFile);

                // Upload a file to the share.
                Console.WriteLine("Uploading file {0} to share", testFile);

                // Set up the name and path of the local file.
                string sourceFile = Path.Combine(localFolder, testFile);
                if (File.Exists(sourceFile))
                {
                    // Upload from the local file to the file share in azure.
                    await cloudFile.UploadFromFileAsync(sourceFile);
                    Console.WriteLine("    Successfully uploaded file to share.");
                }
                else
                {
                    Console.WriteLine("File not found, so not uploaded.");
                }

                //***** Get list of all files/directories on the file share*****//

                // List all files/directories under the root directory.
                Console.WriteLine("Getting list of all files/directories under the root directory of the share.");

                IEnumerable<IListFileItem> fileList = cloudFileShare.GetRootDirectoryReference().ListFilesAndDirectories();

                // Print all files/directories listed above.
                foreach (IListFileItem listItem in fileList)
                {
                    // listItem type will be CloudFile or CloudFileDirectory.
                    Console.WriteLine("    - {0} (type: {1})", listItem.Uri, listItem.GetType());
                }

                Console.WriteLine("Getting list of all files/directories in the file directory on the share.");

                // Now get the list of all files/directories in your directory.
                // Ordinarily, you'd write something recursive to do this for all directories and subdirectories.

                fileList = fileDirectory.ListFilesAndDirectories();

                // Print all files/directories in the folder.
                foreach (IListFileItem listItem in fileList)
                {
                    // listItem type will be CloudFile or CloudFileDirectory.
                    Console.WriteLine("    - {0} (type: {1})", listItem.Uri, listItem.GetType());
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
                await cloudFile.DownloadToFileAsync(Path.Combine(downloadFolder, testFile), FileMode.OpenOrCreate);
                Console.WriteLine("    Successfully downloaded file from share to local temp folder.");

                //***** Copy a file from the file share to blob storage, then abort the copy *****//

                // Copies can sometimes complete before there's a chance to abort. 
                // If that happens with the file you're testing with, try copying the file 
                //   to a storage account in a different region. If it still finishes too fast,
                //   try using a bigger file and copying it to a different region. That will almost always 
                //   take long enough to give you time to abort the copy. 
                // If you want to change the file you're testing the Copy with without changing the value for the 
                //   rest of the sample code, upload the file to the share, then assign the name of the file 
                //   to the testFile variable right here before calling GetFileReference. 
                //   Then it will use the new file for the copy and abort but the rest of the code
                //   will still use the original file.
                CloudFile cloudFileCopy = fileDirectory.GetFileReference(testFile);

                // Upload a file to the share.
                Console.WriteLine("Uploading file {0} to share", testFile);

                // Set up the name and path of the local file.
                string sourceFileCopy = Path.Combine(localFolder, testFile);
                await cloudFileCopy.UploadFromFileAsync(sourceFileCopy);
                Console.WriteLine("    Successfully uploaded file to share.");

                // Copy the file to blob storage.
                Console.WriteLine("Copying file to blob storage. Container name = {0}", shareName);

                // First get a reference to the blob. 
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                // Get a reference to the blob container and create it if it doesn't already exist.
                cloudBlobContainer = cloudBlobClient.GetContainerReference(shareName);
                cloudBlobContainer.CreateIfNotExists();

                // Get a blob reference to the target blob.
                targetBlob = cloudBlobContainer.GetBlobReference(testFile);

                string copyId = string.Empty;

                // Get a reference to the file to be copied.
                cloudFile = fileDirectory.GetFileReference(testFile);

                // Create a SAS for the file that's valid for 24 hours.
                // Note that when you are copying a file to a blob, or a blob to a file, you must use a SAS
                // to authenticate access to the source object, even if you are copying within the same
                // storage account.
                string fileSas = cloudFile.GetSharedAccessSignature(new SharedAccessFilePolicy()
                {
                    // Only read permissions are required for the source file.
                    Permissions = SharedAccessFilePermissions.Read,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24)
                });

                // Construct the URI to the source file, including the SAS token.
                Uri fileSasUri = new Uri(cloudFile.StorageUri.PrimaryUri.ToString() + fileSas);

                // Start the copy of the file to the blob.
                copyId = await targetBlob.StartCopyAsync(fileSasUri);
                Console.WriteLine("   File copy started successfully. copyID = {0}", copyId);

/*              // Abort the copy of the file to blob storage.
                // Note that you call Abort on the target object, i.e. the blob, not the file.
                // If you were copying from one file to another on the file share, the target object would be a file.
                Console.WriteLine("Check the copy status. If pending, cancel the copy operation");

                // Print out the copy state information.
                targetBlob.FetchAttributes();
                Console.WriteLine("    targetBlob.copystate.CopyId = {0}", targetBlob.CopyState.CopyId);
                Console.WriteLine("    targetBlob.copystate.Status = {0}", targetBlob.CopyState.Status);

                // Do the actual abort copy.
                // This only works if the copy is still pending or ongoing.
                if (targetBlob.CopyState.Status == CopyStatus.Pending)
                {
                    Console.WriteLine("   Status is Pending; cancelling the copy operation.");
                    // Call to stop the copy, passing in the copyId of the operation.
                    // This won't work if it has already finished copying.
                    await targetBlob.AbortCopyAsync(copyId);
                    Console.WriteLine("   Cancelling the copy succeeded.");
                }
                else
                {
                    // If this happens, try a larger file.
                    Console.WriteLine("    Cancellation of copy not performed; copy has already finished.");
                }
*/
                // Now clean up after yourself.
                Console.WriteLine("Deleting the files from the file share.");

                // Delete the files because cloudFile is a different file in the range sample.
                cloudFile = fileDirectory.GetFileReference(testFile);
                cloudFile.DeleteIfExists();

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
                cloudFile = fileDirectory.GetFileReference(destFile);

                // Create a string with 512 a's in it. This will be used to write the range.
                int testStreamLen = 512;
                string textToStream = string.Empty;
                textToStream = textToStream.PadRight(testStreamLen, 'a');

                // Name to be used for the file when downloading it so you can inspect it locally
                string downloadFile;

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
                    if (!cloudFile.Exists())
                    {
                        Console.WriteLine("File doesn't exist, create empty file to write ranges to.");

                        // Create a file with a maximum file size of 64k. 
                        await cloudFile.CreateAsync(maxFileSize);
                        Console.WriteLine("    Empty file created successfully.");
                    }

                    // Write the stream to the file starting at startOffset for the length of the stream.
                    Console.WriteLine("Writing range to file.");
                    await cloudFile.WriteRangeAsync(ms, startOffset, null);

                    // Download the file to your temp directory so you can inspect it locally.
                    downloadFile = Path.Combine(downloadFolder, "__testrange.txt");
                    Console.WriteLine("Downloading file to examine.");
                    await cloudFile.DownloadToFileAsync(downloadFile, FileMode.OpenOrCreate);
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
                    await cloudFile.WriteRangeAsync(ms, startOffset, null);
                    Console.WriteLine("    Successful writing second range to file.");

                    // Download the file to your temp directory so you can examine it.
                    downloadFile = Path.Combine(downloadFolder, "__testrange2.txt");
                    Console.WriteLine("Downloading file with two ranges in it to examine.");
                    await cloudFile.DownloadToFileAsync(downloadFile, FileMode.OpenOrCreate);
                    Console.WriteLine("    Successfully downloaded file to examine.");
                }

                // Query and view the list of ranges.
                Console.WriteLine("Call to get the list of ranges.");
                IEnumerable<FileRange> listOfRanges = await cloudFile.ListRangesAsync();
                Console.WriteLine("    Successfully retrieved list of ranges.");
                foreach (FileRange fileRange in listOfRanges)
                {
                    Console.WriteLine("    --> filerange startOffset = {0}, endOffset = {1}", fileRange.StartOffset, fileRange.EndOffset);
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
                cloudFile = fileDirectory.GetFileReference(destFile);
                await cloudFile.DeleteIfExistsAsync();

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
                await cloudFileShare.DeleteAsync();
                Console.WriteLine("    Deleted the file share successfully.");

                Console.WriteLine("Deleting the temporary download directory and the file in it.");

                // Delete the download folder and its contents.
                Directory.Delete(downloadFolder, true);
                Console.WriteLine("    Successfully deleted the temporary download directory.");

                Console.WriteLine("Deleting the container and blob used in the Copy/Abort test.");
                await targetBlob.DeleteIfExistsAsync();
                await cloudBlobContainer.DeleteIfExistsAsync();
                Console.WriteLine("    Successfully deleted the blob and its container.");

            }

        }

    }
}
