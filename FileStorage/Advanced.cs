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
            // Keep a list of the file shares so you can compare this list 
            //   against the list of shares that we retrieve .
            List<string> fileShareNames = new List<string>();
            // Create a file client for interacting with the file service.
            CloudFileClient cloudFileClient = null;

            try
            {
                //***** Setup *****//
                Console.WriteLine("Getting reference to the storage account.");

                // Retrieve storage account information from connection string
                // How to create a storage connection string - http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
                CloudStorageAccount storageAccount = Common.CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

                Console.WriteLine("Instantiating file client.");
                Console.WriteLine(string.Empty);

                // Create a file client for interacting with the file service.
                cloudFileClient = storageAccount.CreateCloudFileClient();

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
                Console.WriteLine("    Exception thrown. Message = {0}{1}    Strack Trace = {2}", ex.Message, Environment.NewLine, ex.StackTrace);
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

        }

    }
}
