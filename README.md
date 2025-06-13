# Introduction 
DataMigrationService is a tool for managing iMIS data migrations.

# License
DataMigrationService is open source software, licensed under the terms of the MIT License.

# Required Tools / Software
- .Net 8.0 SDK. [Link](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Microsoft SQL Server (2012 or later) [Link](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)

# Run
**Note: The tool requires it's own database and is configured to create the database by running Entity Framework migrations during startup. This means the tool should not be hosted
in a multi-instance environment and requires elevated permissions to the database (See details [here](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli#apply-migrations-at-runtime)).
If you intend to use the tool in a manner incompatible with these constraints, you should remove the migrations from startup and manage them using one of the other documented methods.**

1. Clone the repo to your desired location
2. Update the appsettings.json file to provide a valid Microsoft SQL Server connection string for the `DefaultConnectionString` value in the `ConnectionStrings` section: 
```json
    "ConnectionStrings": {
        "DefaultConnection": "Your-ConnectionString-Here"
    },
```
3. Open a command prompt or powershell prompt and navigate to the project location. 
4. Run the project using `dotnet run`. Options for dotnet run can be found [here](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run).
```ps
dotnet run --project ./Asi.DataMigrationService/Asi.DataMigrationService.csproj --property:Configuration=Release
```
5. Open your preferred browser and navigate to the URL in the dotnet run output. (Typically https://localhost:5001.)