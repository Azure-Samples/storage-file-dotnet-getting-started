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

using System;

namespace FileStorage
{

    /// <summary>
    /// Azure Storage File Sample - Demonstrate how to use the File Storage service. 
    /// 
    /// Note: This sample uses the .NET 4.5 asynchronous programming model to demonstrate how to call the Storage Service using the 
    /// storage client libraries asynchronous APIs. When used in real applications this approach enables you to improve the 
    /// responsiveness of your application. Calls to the storage service are prefixed by the await keyword. 
    /// 
    /// Documentation References: 
    /// - What is a Storage Account - http://azure.microsoft.com/en-us/documentation/articles/storage-whatis-account/
    /// - Getting Started with Files - http://blogs.msdn.com/b/windowsazurestorage/archive/2014/05/12/introducing-microsoft-azure-file-service.aspx
    /// - How to use Azure File Storage - http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-files/
    /// - File Service Concepts - http://msdn.microsoft.com/en-us/library/dn166972.aspx
    /// - File Service REST API - http://msdn.microsoft.com/en-us/library/dn167006.aspx
    /// - File Service C# API - http://msdn.microsoft.com/en-us/library/microsoft.windowsazure.storage.file.aspx
    /// - Asynchronous Programming with Async and Await  - http://msdn.microsoft.com/en-us/library/hh191443.aspx
    /// </summary>
    public class Program
    {
        // *************************************************************************************************************************
        // Instructions: This sample can be run against Microsoft Azure Storage Service by updating the App.Config with your AccountName and AccountKey. 
        // 
        // To run the sample using the Storage Service     
        //      1. Create a Storage Account through the Azure Portal and provide your [AccountName] and [AccountKey] in 
        //         the App.Config file. See http://go.microsoft.com/fwlink/?LinkId=325277 for more information.
        //      2. Set breakpoints and run the project using F10. 
        // 
        // *************************************************************************************************************************        
        static void Main(string[] args)
        {
            Console.WriteLine("Azure Storage File Sample\n ");

            // Create share, upload file, download file, list files and folders, copy file, abort copy file, write range, list ranges.
            GettingStarted getStarted = new GettingStarted();
            getStarted.RunFileStorageOperationsAsync().Wait();

            //create 3 file shares, then show how to list all the file shares in the storage account
            Advanced advancedOps = new Advanced();
            advancedOps.RunFileStorageAdvancedOpsAsync().Wait();

            Console.WriteLine(string.Empty);
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }


    }
}
