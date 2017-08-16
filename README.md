# Code Challenge Function App

This C# application defines two Azure functions designed to process data updates for the Code Challenge app:
  - UpdateExclusionsList
  - UploadRecords

### Why Azure Functions
The Code Challenge app uses an Azure CosmosDB database to store its data. Using Azure Functions to update this data provides a simple, serverless, secure way to update this data.

### UpdateExclusionsList
This function runs on a timer (currently 6am every day) to query the latest exclusions list and update it in the database.

### UploadRecords
This function is triggered by an HTTP POST request containing an array of WebsiteVisitsRecords in its body. The function will add new records and discard records that are already present in the database.

Only clients with the specific function key will be able to call this function, which provides an additional layer of security

### Deployment
There are two main ways to deploy Azure Functions apps:
 - Continuous deployment from source code repository'
 - Deployment from Visual Studio

For this simple app we used the Visual Studio deployment functionality, which is easier to setup and allows to automatically deploy a compiled binary version of the app (including its dependencies).

### Configuration
Azure Function apps are configured from the Azure web portal, like other Azure Web apps