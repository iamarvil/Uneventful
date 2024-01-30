## TodoApp.Cosmos

This example project shows pretty much what the `Uneventful` library does while utilizing the built-in CosmosDB Change feed processor for event processing.

### What you'll need

 - The whole project targets dotnet 8 so you must have [the dotnet 8 SDK](https://dotnet.microsoft.com/en-us/download) installed.
 - [CosmosDB Emulator](https://aka.ms/cosmosdb-emulator) or an Azure Cosmos DB account with a database and container to be used for testing.

### Setup

1. Ensure CosmosDB Emulator is running by visiting the CosmosDB Emulator's Document Explorer on your browser at https://localhost:8081/_explorer/index.html.
2. The example app automatically creates the database and containers for you, here are the details of the database and containers it will create:
    - **Database**: `TodoApp`
      - **Container**: `todo-events`
        - **Partition key**: `/streamId`
        - **Container throughput**: `400 RU/s`
        - **Indexing policy**:
          ```json
          {
           "indexingMode": "consistent",
           "automatic": true,
           "includedPaths": [
             { "path": "/streamId/?" }
           ],
           "excludedPaths": [
             { "path": "/*" }
           ],
           "compositeIndexes": [
             [
                { "path": "/streamId" },
                {
                   "path": "/version",
                   "order": "ascending"
                }
             ]
           ]
          }
          ```
    - **Container**: `todo-events-leases`
        - **Partition key**: `/id`
        - **Container throughput**: `400 RU/s`
        - **Indexing policy**: default

From here, you can run the `TodoApp.Cosmos` project and a SwaggerUI tab will be opened on your browser.