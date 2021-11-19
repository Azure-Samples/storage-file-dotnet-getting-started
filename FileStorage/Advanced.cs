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
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

// Create several shares, then show how to list them
namespace FileStorage
{
    public class Advanced
    {
        /// <summary>
        /// Test some of the file storage operations.
        /// </summary>
        public async Task RunFileStorageAdvancedOpsAsync()
        {
            //***** Setup *****//
            Console.WriteLine("Getting reference to the storage account.");

            // How to create a storage connection string - http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
            string storageConnectionString = ConfigurationManager.AppSettings.Get("StorageConnectionString");

            Console.WriteLine("Instantiating file client.");
            Console.WriteLine(string.Empty);

            // Create a share service client for interacting with the file service.
            ShareServiceClient shareServiceClient = new ShareServiceClient(storageConnectionString);

            // List shares
            await ListSharesSample(shareServiceClient);

            // CORS Rules
            await CorsSample(shareServiceClient);

            // Share Properties
            await SharePropertiesSample(shareServiceClient);

            // Share Metadata
            await ShareMetadataSample(shareServiceClient);

            // Directory Properties
            await DirectoryPropertiesSample(shareServiceClient);

            // Directory Metadata
            await DirectoryMetadataSample(shareServiceClient);

            // File Properties
            await FilePropertiesSample(shareServiceClient);

            // File Metadata
            await FileMetadataSample(shareServiceClient);
        }

        private static async Task ListSharesSample(ShareServiceClient shareServiceClient)
        {
            Console.WriteLine();

            // Keep a list of the file shares so you can compare this list 
            //   against the list of shares that we retrieve .
            List<string> fileShareNames = new List<string>();
            try
            {
                // Create 3 file shares.

                // Create the share name -- use a guid in the name so it's unique.
                // This will also be used as the container name for blob storage when copying the file to blob storage.
                string baseShareName = "demotest-" + System.Guid.NewGuid().ToString();

                for (int i = 0; i < 3; i++)
                {
                    // Set the name of the share, then add it to the generic list.
                    string shareName = baseShareName + "-0" + i;
                    fileShareNames.Add(shareName);

                    // Create the share with this name.
                    Console.WriteLine("Creating share with name {0}", shareName);
                    ShareClient shareClient = shareServiceClient.GetShareClient(shareName);
                    try
                    {
                        await shareClient.CreateIfNotExistsAsync();
                        Console.WriteLine("    Share created successfully.");
                    }
                    catch (RequestFailedException exRequest)
                    {
                        Common.WriteException(exRequest);
                        Console.WriteLine(
                            "Please make sure your storage account has storage file endpoint enabled and specified correctly in the app.config - then restart the sample.");
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
                }

                Console.WriteLine(string.Empty);
                Console.WriteLine("List of shares in the storage account:");

                // List the file shares for this storage account 
                var shareList = shareServiceClient.GetSharesAsync();
                try
                {
                    await foreach (ShareItem Share in shareList)
                    {
                        Console.WriteLine("Cloud Share name = {0}", Share.Name);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("    Exception thrown listing shares.");
                    Common.WriteException(ex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("    Exception thrown. Message = {0}{1}    Strack Trace = {2}", ex.Message,
                    Environment.NewLine, ex.StackTrace);
            }
            finally
            {
                // If it created the file shares, remove them (cleanup).
                if (fileShareNames != null && shareServiceClient != null)
                {
                    // Now clean up after yourself, using the list of shares that you created in case there were other shares in the account.
                    foreach (string fileShareName in fileShareNames)
                    {
                        ShareClient share = shareServiceClient.GetShareClient(fileShareName);
                        share.DeleteIfExists();
                    }
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Query the Cross-Origin Resource Sharing (CORS) rules for the File service
        /// </summary>
        /// <param name="shareServiceClient"></param>
        private static async Task CorsSample(ShareServiceClient shareServiceClient)
        {
            Console.WriteLine();

            // Get service properties
            Console.WriteLine("Get service properties");
            ShareServiceProperties originalProperties = await shareServiceClient.GetPropertiesAsync();
            try
            {
                // Add CORS rule
                Console.WriteLine("Add CORS rule");

                ShareCorsRule corsRule = new ShareCorsRule
                {
                    AllowedHeaders = "*",
                    AllowedMethods = "GET",
                    AllowedOrigins = "*",
                    ExposedHeaders = "*",
                    MaxAgeInSeconds = 3600
                };

                ShareServiceProperties serviceProperties = await shareServiceClient.GetPropertiesAsync();

                serviceProperties.Cors.Clear();
                serviceProperties.Cors.Add(corsRule);

                await shareServiceClient.SetPropertiesAsync(serviceProperties);
                Console.WriteLine("Set property successfully");
            }
            finally
            {
                // Revert back to original service properties
                Console.WriteLine("Revert back to original service properties");
                await shareServiceClient.SetPropertiesAsync(originalProperties);
                Console.WriteLine("Revert properties successfully.");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Manage share properties
        /// </summary>
        /// <param name="shareServiceClient"></param>
        /// <returns></returns>
        private static async Task SharePropertiesSample(ShareServiceClient shareServiceClient)
        {
            Console.WriteLine();

            // Create the share name -- use a guid in the name so it's unique.
            string shareName = "demotest-" + Guid.NewGuid();
            ShareClient shareClient = shareServiceClient.GetShareClient(shareName);
            try
            {
                // Create Share
                Console.WriteLine("Create Share");
                await shareClient.CreateIfNotExistsAsync();

                // Set share properties
                Console.WriteLine("Set share properties");
                await shareClient.SetQuotaAsync(100);

                // Fetch share properties
                ShareProperties shareProperties = await shareClient.GetPropertiesAsync();
                Console.WriteLine("Get share properties:");
                Console.WriteLine("    Quota: {0}", shareProperties.QuotaInGB);
                Console.WriteLine("    ETag: {0}", shareProperties.ETag);
                Console.WriteLine("    Last modified: {0}", shareProperties.LastModified);
            }
            catch (RequestFailedException exRequest)
            {
                Common.WriteException(exRequest);
                Console.WriteLine(
                    "Please make sure your storage account is specified correctly in the app.config - then restart the sample.");
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
            finally
            {
                // Delete share
                Console.WriteLine("Delete share");
                await shareClient.DeleteIfExistsAsync();
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Manage share metadata
        /// </summary>
        /// <param name="shareServiceClient"></param>
        /// <returns></returns>
        private static async Task ShareMetadataSample(ShareServiceClient shareServiceClient)
        {
            Console.WriteLine();

            // Create the share name -- use a guid in the name so it's unique.
            string shareName = "demotest-" + Guid.NewGuid();

            // Create Share
            Console.WriteLine("Create Share");

            ShareClient shareClient = shareServiceClient.GetShareClient(shareName);
            await shareClient.CreateIfNotExistsAsync();

            // Set share metadata
            Console.WriteLine("Set share metadata");

            Dictionary<string, string> metaData = new Dictionary<string, string>();
            metaData.Add("key1", "key1");
            metaData.Add("key2", "key2");
            await shareClient.SetMetadataAsync(metaData);
            try
            {
                // Create Share
                Console.WriteLine("Create Share");
                await shareClient.CreateIfNotExistsAsync();

                // Fetch share attributes
                ShareProperties shareProperties = await shareClient.GetPropertiesAsync();
                Console.WriteLine("Get share metadata:");
                foreach (var keyValue in shareProperties.Metadata)
                {
                    Console.WriteLine("    {0}: {1}", keyValue.Key, keyValue.Value);
                }
                Console.WriteLine();
            }
            catch (RequestFailedException exRequest)
            {
                Common.WriteException(exRequest);
                Console.WriteLine(
                    "Please make sure your storage account is specified correctly in the app.config - then restart the sample.");
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
            finally
            {
                // Delete share
                Console.WriteLine("Delete share");
                await shareClient.DeleteIfExistsAsync();
            }
        }

        /// <summary>
        /// Get directory properties
        /// </summary>
        /// <param name="shareServiceClient"></param>
        /// <returns></returns>
        private static async Task DirectoryPropertiesSample(ShareServiceClient shareServiceClient)
        {
            Console.WriteLine();

            // Create the share name -- use a guid in the name so it's unique.
            string shareName = "demotest-" + Guid.NewGuid();
            ShareClient shareClient = shareServiceClient.GetShareClient(shareName);
            try
            {
                // Create share
                Console.WriteLine("Create Share");
                await shareClient.CreateIfNotExistsAsync();

                // Create directory
                Console.WriteLine("Create directory");
                ShareDirectoryClient rootDirectory = shareClient.GetRootDirectoryClient();
                
                // Fetch directory attributes
                ShareDirectoryProperties shareDirectoryProperties = await rootDirectory.GetPropertiesAsync();
                Console.WriteLine("Get directory properties:");
                Console.WriteLine("    ETag: {0}", shareDirectoryProperties.ETag);
                Console.WriteLine("    Last modified: {0}", shareDirectoryProperties.LastModified);
            }
            catch (RequestFailedException exRequest)
            {
                Common.WriteException(exRequest);
                Console.WriteLine(
                    "Please make sure your storage account is specified correctly in the app.config - then restart the sample.");
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
            finally
            {
                // Delete share
                Console.WriteLine("Delete share");
                await shareClient.DeleteIfExistsAsync();
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Manage share metadata
        /// </summary>
        /// <param name="shareServiceClient"></param>
        /// <returns></returns>
        private static async Task DirectoryMetadataSample(ShareServiceClient shareServiceClient)
        {
            Console.WriteLine();

            // Create the share name -- use a GUID in the name so it's unique.
            string shareName = "demotest-" + Guid.NewGuid();
            ShareClient shareClient = shareServiceClient.GetShareClient(shareName);

            try
            {
                // Create share
                Console.WriteLine("Create Share");
                await shareClient.CreateIfNotExistsAsync();

                ShareDirectoryClient rootDirectory = shareClient.GetRootDirectoryClient();

                Console.WriteLine("Create the directory");
                // Get a directory client
                ShareDirectoryClient sampleDirectory = rootDirectory.GetSubdirectoryClient("sample-directory");
                await sampleDirectory.CreateIfNotExistsAsync();

                // Set directory metadata
                Console.WriteLine("Set directory metadata");

                Dictionary<string, string> metadate = new Dictionary<string, string>();
                metadate.Add("key1", "value1");
                metadate.Add("key2", "value2");
                await sampleDirectory.SetMetadataAsync(metadate);

                // Fetch directory attributes
                ShareDirectoryProperties shareDirectoryProperties = await sampleDirectory.GetPropertiesAsync();

                Console.WriteLine("Get directory metadata:");
                foreach (var keyValue in shareDirectoryProperties.Metadata)
                {
                    Console.WriteLine("    {0}: {1}", keyValue.Key, keyValue.Value);
                }
                Console.WriteLine();
            }
            catch (RequestFailedException exRequest)
            {
                Common.WriteException(exRequest);
                Console.WriteLine(
                    "Please make sure your storage account is specified correctly in the app.config - then restart the sample.");
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
            finally
            {
                // Delete share
                Console.WriteLine("Delete share");
                await shareClient.DeleteIfExistsAsync();
            }
        }

        /// <summary>
        /// Manage file properties
        /// </summary>
        /// <param name="shareServiceClient"></param>
        /// <returns></returns>
        private static async Task FilePropertiesSample(ShareServiceClient shareServiceClient)
        {
            Console.WriteLine();

            // Create the share name -- use a guid in the name so it's unique.
            string shareName = "demotest-" + Guid.NewGuid();
            ShareClient shareClient = shareServiceClient.GetShareClient(shareName);
            try
            {
                // Create share
                Console.WriteLine("Create Share");
                await shareClient.CreateIfNotExistsAsync();

                // Create directory
                Console.WriteLine("Create directory");
                ShareDirectoryClient rootDirectory = shareClient.GetRootDirectoryClient();
                
                ShareFileClient file = rootDirectory.GetFileClient("demofile");

                // Create file
                Console.WriteLine("Create file");
                await file.CreateAsync(1000);

                // Set file properties
                ShareFileHttpHeaders headers = new ShareFileHttpHeaders()
                {
                    ContentType = "plain/text",
                    ContentEncoding = new string[] { "UTF-8" },
                    ContentLanguage = new string[] { "en" }
                };

                await file.SetHttpHeadersAsync(httpHeaders: headers);

                // Fetch file attributes
                ShareFileProperties shareFileProperties = await file.GetPropertiesAsync();
                Console.WriteLine("Get file properties:");
                Console.WriteLine("    Content type: {0}", shareFileProperties.ContentType);
                Console.WriteLine("    Content encoding: {0}", string.Join("", shareFileProperties.ContentEncoding));
                Console.WriteLine("    Content language: {0}", string.Join("", shareFileProperties.ContentLanguage));
                Console.WriteLine("    Length: {0}", shareFileProperties.ContentLength);
            }
            catch (RequestFailedException exRequest)
            {
                Common.WriteException(exRequest);
                Console.WriteLine(
                    "Please make sure your storage account is specified correctly in the app.config - then restart the sample.");
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
            finally
            {
                // Delete share
                Console.WriteLine("Delete share");
                await shareClient.DeleteIfExistsAsync();
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Manage file metadata
        /// </summary>
        /// <param name="shareServiceClient"></param>
        /// <returns></returns>
        private static async Task FileMetadataSample(ShareServiceClient shareServiceClient)
        {
            Console.WriteLine();

            // Create the share name -- use a guid in the name so it's unique.
            string shareName = "demotest-" + Guid.NewGuid();
            ShareClient shareClient = shareServiceClient.GetShareClient(shareName);
            try
            {
                // Create share
                Console.WriteLine("Create Share");
                await shareClient.CreateIfNotExistsAsync();

                // Create directory
                Console.WriteLine("Create directory");
                ShareDirectoryClient rootDirectory = shareClient.GetRootDirectoryClient();
               
                ShareFileClient file = rootDirectory.GetFileClient("demofile");

                // Create file
                Console.WriteLine("Create file");
                await file.CreateAsync(1000);

                // Set file metadata
                Console.WriteLine("Set file metadata");
                Dictionary<string, string> metadata = new Dictionary<string, string>();
                metadata.Add("key1", "value1");
                metadata.Add("key2", "value2");

                await file.SetMetadataAsync(metadata);

                // Fetch file attributes
                ShareFileProperties properties = await file.GetPropertiesAsync();
                Console.WriteLine("Get file metadata:");
                foreach (var keyValue in properties.Metadata)
                {
                    Console.WriteLine("    {0}: {1}", keyValue.Key, keyValue.Value);
                }
                Console.WriteLine();
            }
            catch (RequestFailedException exRequest)
            {
                Common.WriteException(exRequest);
                Console.WriteLine(
                    "Please make sure your storage account is specified correctly in the app.config - then restart the sample.");
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
            finally
            {
                // Delete share
                Console.WriteLine("Delete share");
                await shareClient.DeleteIfExistsAsync();
            }
        }
    }
}
