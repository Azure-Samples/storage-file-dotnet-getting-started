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
using Microsoft.WindowsAzure.Storage.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.File.Protocol;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;


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

            // Retrieve storage account information from connection string
            // How to create a storage connection string - http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
            CloudStorageAccount storageAccount = Common.CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            Console.WriteLine("Instantiating file client.");
            Console.WriteLine(string.Empty);

            // Create a file client for interacting with the file service.
            CloudFileClient cloudFileClient = storageAccount.CreateCloudFileClient();

            // List shares
            await ListSharesSample(cloudFileClient);

            // CORS Rules
            await CorsSample(cloudFileClient);

            // Share Properties
            await SharePropertiesSample(cloudFileClient);

            // Share Metadata
            await ShareMetadataSample(cloudFileClient);

            // Directory Properties
            await DirectoryPropertiesSample(cloudFileClient);

            // Directory Metadata
            await DirectoryMetadataSample(cloudFileClient);

            // File Properties
            await FilePropertiesSample(cloudFileClient);

            // File Metadata
            await FileMetadataSample(cloudFileClient);
        }

        private static async Task ListSharesSample(CloudFileClient cloudFileClient)
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
                    CloudFileShare cloudFileShare = cloudFileClient.GetShareReference(shareName);
                    try
                    {
                        await cloudFileShare.CreateIfNotExistsAsync();
                        Console.WriteLine("    Share created successfully.");
                    }
                    catch (StorageException exStorage)
                    {
                        Common.WriteException(exStorage);
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
                IEnumerable<CloudFileShare> cloudShareList = cloudFileClient.ListShares();
                try
                {
                    foreach (CloudFileShare cloudShare in cloudShareList)
                    {
                        Console.WriteLine("Cloud Share name = {0}", cloudShare.Name);
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
                if (fileShareNames != null && cloudFileClient != null)
                {
                    // Now clean up after yourself, using the list of shares that you created in case there were other shares in the account.
                    foreach (string fileShareName in fileShareNames)
                    {
                        CloudFileShare cloudFileShare = cloudFileClient.GetShareReference(fileShareName);
                        cloudFileShare.DeleteIfExists();
                    }
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Query the Cross-Origin Resource Sharing (CORS) rules for the File service
        /// </summary>
        /// <param name="fileClient"></param>
        private static async Task CorsSample(CloudFileClient fileClient)
        {
            Console.WriteLine();

            // Get service properties
            Console.WriteLine("Get service properties");
            FileServiceProperties originalProperties = await fileClient.GetServicePropertiesAsync();
            try
            {
                // Set CORS rules
                Console.WriteLine("Set CORS rules");

                CorsProperties cors = new CorsProperties();
                CorsRule corsRule = new CorsRule
                {
                    AllowedHeaders = new List<string> { "*" },
                    AllowedMethods = CorsHttpMethods.Get,
                    AllowedOrigins = new List<string> { "*" },
                    ExposedHeaders = new List<string> { "*" },
                    MaxAgeInSeconds = 3600
                };

                cors.CorsRules.Add(corsRule);
                FileServiceProperties serviceProperties = await fileClient.GetServicePropertiesAsync();
                serviceProperties.Cors = cors;
                await fileClient.SetServicePropertiesAsync(serviceProperties);
            }
            finally
            {
                // Revert back to original service properties
                Console.WriteLine("Revert back to original service properties");
                await fileClient.SetServicePropertiesAsync(originalProperties);
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Manage share properties
        /// </summary>
        /// <param name="cloudFileClient"></param>
        /// <returns></returns>
        private static async Task SharePropertiesSample(CloudFileClient cloudFileClient)
        {
            Console.WriteLine();
            // Create the share name -- use a guid in the name so it's unique.
            string shareName = "demotest-" + Guid.NewGuid();

            CloudFileShare share = cloudFileClient.GetShareReference(shareName);

            // Set share properties
            Console.WriteLine("Set share properties");
            share.Properties.Quota = 100;

            try
            {
                // Create Share
                Console.WriteLine("Create Share");
                await share.CreateIfNotExistsAsync();

                // Fetch share attributes
                // in this case this call is not need but is included for demo purposes
                await share.FetchAttributesAsync();
                Console.WriteLine("Get share properties:");
                Console.WriteLine("    Quota: {0}", share.Properties.Quota);
                Console.WriteLine("    ETag: {0}", share.Properties.ETag);
                Console.WriteLine("    Last modified: {0}", share.Properties.LastModified);
            }
            catch (StorageException exStorage)
            {
                Common.WriteException(exStorage);
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
                share.DeleteIfExists();
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Manage share metadata
        /// </summary>
        /// <param name="cloudFileClient"></param>
        /// <returns></returns>
        private static async Task ShareMetadataSample(CloudFileClient cloudFileClient)
        {
            Console.WriteLine();
            // Create the share name -- use a guid in the name so it's unique.
            string shareName = "demotest-" + Guid.NewGuid();

            // Create the share with this name.
            CloudFileShare share = cloudFileClient.GetShareReference(shareName);

            // Set share metadata
            Console.WriteLine("Set share metadata");
            share.Metadata.Add("key1", "value1");
            share.Metadata.Add("key2", "value2");

            try
            {
                // Create Share
                Console.WriteLine("Create Share");
                await share.CreateIfNotExistsAsync();

                // Fetch share attributes
                // in this case this call is not need but is included for demo purposes
                await share.FetchAttributesAsync();
                Console.WriteLine("Get share metadata:");
                foreach (var keyValue in share.Metadata)
                {
                    Console.WriteLine("    {0}: {1}", keyValue.Key, keyValue.Value);
                }
                Console.WriteLine();
            }
            catch (StorageException exStorage)
            {
                Common.WriteException(exStorage);
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
                share.DeleteIfExists();
            }
        }

        /// <summary>
        /// Get directory properties
        /// </summary>
        /// <param name="cloudFileClient"></param>
        /// <returns></returns>
        private static async Task DirectoryPropertiesSample(CloudFileClient cloudFileClient)
        {
            Console.WriteLine();
            // Create the share name -- use a guid in the name so it's unique.
            string shareName = "demotest-" + Guid.NewGuid();
            CloudFileShare share = cloudFileClient.GetShareReference(shareName);
            try
            {
                // Create share
                Console.WriteLine("Create Share");
                await share.CreateIfNotExistsAsync();

                // Create directory
                Console.WriteLine("Create directory");
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
                await rootDirectory.CreateIfNotExistsAsync();
                
                // Fetch directory attributes
                // in this case this call is not need but is included for demo purposes
                await rootDirectory.FetchAttributesAsync();
                Console.WriteLine("Get directory properties:");
                Console.WriteLine("    ETag: {0}", rootDirectory.Properties.ETag);
                Console.WriteLine("    Last modified: {0}", rootDirectory.Properties.LastModified);
            }
            catch (StorageException exStorage)
            {
                Common.WriteException(exStorage);
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
                share.DeleteIfExists();
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Manage share metadata
        /// </summary>
        /// <param name="cloudFileClient"></param>
        /// <returns></returns>
        private static async Task DirectoryMetadataSample(CloudFileClient cloudFileClient)
        {
            Console.WriteLine();
            // Create the share name -- use a guid in the name so it's unique.
            string shareName = "demotest-" + Guid.NewGuid();
            CloudFileShare share = cloudFileClient.GetShareReference(shareName);
            try
            {
                // Create share
                Console.WriteLine("Create Share");
                await share.CreateIfNotExistsAsync();

                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();

                // Create directory
                Console.WriteLine("Create directory");
                await rootDirectory.CreateIfNotExistsAsync();

                // Set directory metadata
                Console.WriteLine("Set directory metadata");
                rootDirectory.Metadata.Add("key1", "value1");
                rootDirectory.Metadata.Add("key2", "value2");
                await rootDirectory.SetMetadataAsync();

                // Fetch directory attributes
                // in this case this call is not need but is included for demo purposes
                await rootDirectory.FetchAttributesAsync();
                Console.WriteLine("Get directory metadata:");
                foreach (var keyValue in rootDirectory.Metadata)
                {
                    Console.WriteLine("    {0}: {1}", keyValue.Key, keyValue.Value);
                }
                Console.WriteLine();
            }
            catch (StorageException exStorage)
            {
                Common.WriteException(exStorage);
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
                share.DeleteIfExists();
            }
        }

        /// <summary>
        /// Manage file properties
        /// </summary>
        /// <param name="cloudFileClient"></param>
        /// <returns></returns>
        private static async Task FilePropertiesSample(CloudFileClient cloudFileClient)
        {
            Console.WriteLine();
            // Create the share name -- use a guid in the name so it's unique.
            string shareName = "demotest-" + Guid.NewGuid();
            CloudFileShare share = cloudFileClient.GetShareReference(shareName);
            try
            {
                // Create share
                Console.WriteLine("Create Share");
                await share.CreateIfNotExistsAsync();

                // Create directory
                Console.WriteLine("Create directory");
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
                await rootDirectory.CreateIfNotExistsAsync();

                CloudFile file = rootDirectory.GetFileReference("demofile");

                // Set file properties
                file.Properties.ContentType = "plain/text";
                file.Properties.ContentEncoding = "UTF-8";
                file.Properties.ContentLanguage = "en";

                // Create file
                Console.WriteLine("Create file");
                await file.CreateAsync(1000);
                
                // Fetch file attributes
                // in this case this call is not need but is included for demo purposes
                await file.FetchAttributesAsync();
                Console.WriteLine("Get file properties:");
                Console.WriteLine("    ETag: {0}", file.Properties.ETag);
                Console.WriteLine("    Content type: {0}", file.Properties.ContentType);
                Console.WriteLine("    Cache control: {0}", file.Properties.CacheControl);
                Console.WriteLine("    Content encoding: {0}", file.Properties.ContentEncoding);
                Console.WriteLine("    Content language: {0}", file.Properties.ContentLanguage);
                Console.WriteLine("    Content disposition: {0}", file.Properties.ContentDisposition);
                Console.WriteLine("    Content MD5: {0}", file.Properties.ContentMD5);
                Console.WriteLine("    Length: {0}", file.Properties.Length);
            }
            catch (StorageException exStorage)
            {
                Common.WriteException(exStorage);
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
                share.DeleteIfExists();
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Manage file metadata
        /// </summary>
        /// <param name="cloudFileClient"></param>
        /// <returns></returns>
        private static async Task FileMetadataSample(CloudFileClient cloudFileClient)
        {
            Console.WriteLine();
            // Create the share name -- use a guid in the name so it's unique.
            string shareName = "demotest-" + Guid.NewGuid();
            CloudFileShare share = cloudFileClient.GetShareReference(shareName);
            try
            {
                // Create share
                Console.WriteLine("Create Share");
                await share.CreateIfNotExistsAsync();

                // Create directory
                Console.WriteLine("Create directory");
                CloudFileDirectory rootDirectory = share.GetRootDirectoryReference();
                await rootDirectory.CreateIfNotExistsAsync();

                CloudFile file = rootDirectory.GetFileReference("demofile");

                // Set file metadata
                Console.WriteLine("Set file metadata");
                file.Metadata.Add("key1", "value1");
                file.Metadata.Add("key2", "value2");

                // Create file
                Console.WriteLine("Create file");
                await file.CreateAsync(1000);

                // Fetch file attributes
                // in this case this call is not need but is included for demo purposes
                await file.FetchAttributesAsync();
                Console.WriteLine("Get file metadata:");
                foreach (var keyValue in file.Metadata)
                {
                    Console.WriteLine("    {0}: {1}", keyValue.Key, keyValue.Value);
                }
                Console.WriteLine();
            }
            catch (StorageException exStorage)
            {
                Common.WriteException(exStorage);
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
                share.DeleteIfExists();
            }
        }
    }
}
