---
page_type: sample
languages:
- csharp
products:
- azure
description: "Demonstrates how to use the File Storage service."
urlFragment: storage-file-dotnet-getting-started
---

# Getting Started with Azure File Service in .NET

Demonstrates how to use the File Storage service.

Note: This sample uses the .NET 4.5 asynchronous programming model to demonstrate how to call the Storage Service using the
storage client libraries asynchronous API's. When used in real applications this approach enables you to improve the
responsiveness of your application. Calls to the storage service are prefixed by the await keyword.
If you don't have a Microsoft Azure subscription you can
get a FREE trial account <a href="http://go.microsoft.com/fwlink/?LinkId=330212">here</a>.


## Running this sample
This sample can be run against Microsoft Azure Storage Service by updating the App.Config with your AccountName and AccountKey.

To run the sample using the Storage Service

1. Create a Storage Account through the Azure Portal and provide your [AccountName] and [AccountKey] in the App.Config file.
2. Set breakpoints and run the project using F10.

## More information
- [What is a Storage Account](http://azure.microsoft.com/en-us/documentation/articles/storage-whatis-account/)
- [Getting Started with Files](http://blogs.msdn.com/b/windowsazurestorage/archive/2014/05/12/introducing-microsoft-azure-file-service.aspx)
- [How to use Azure File Storage](http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-files/)
- [File Service Concepts](http://msdn.microsoft.com/en-us/library/dn166972.aspx)
- [File Service REST API](http://msdn.microsoft.com/en-us/library/dn167006.aspx)
- [File Service C# API](http://msdn.microsoft.com/en-us/library/microsoft.windowsazure.storage.file.aspx)
- [Asynchronous Programming with Async and Await](http://msdn.microsoft.com/en-us/library/hh191443.aspx)
