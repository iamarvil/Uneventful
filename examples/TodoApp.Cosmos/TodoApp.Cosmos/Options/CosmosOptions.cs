namespace TodoApp.Cosmos.Options;

public class CosmosOptions {
    public const string CosmosConfig = "Cosmos";
    public const string CosmosEventStoreConfig = "Cosmos:EventStore";
    public const string CosmosLeasesConfig = "Cosmos:Leases";
    
    
    public CosmosOptionGroup EventStore { get; init; } = new();
    public CosmosOptionGroup Leases { get; init; } = new();
    
    public bool NoEventStoreConnectionString => EventStore.NoConnectionString || EventStore.NoAccountEndpointAndKey;
    public bool NoEventStoreDatabaseNameAndContainerName => EventStore.NoDatabaseNameAndContainerName;
    public bool NoLeasesConnectionString => Leases.NoConnectionString || Leases.NoAccountEndpointAndKey;
    public bool NoLeasesDatabaseNameAndContainerName => Leases.NoDatabaseNameAndContainerName;
    
}

public class CosmosOptionGroup {
    private string? _connectionString;
    public string AccountEndpoint { get; init; } = string.Empty;
    public string AccountKey { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = string.Empty;
    public string ContainerName { get; init; } = string.Empty;
    
    public string ConnectionString {
        get => _connectionString ?? $"AccountEndpoint={AccountEndpoint};AccountKey={AccountKey}";
        set => _connectionString = value;
    }
    
    public bool NoConnectionString => string.IsNullOrWhiteSpace(ConnectionString);
    public bool NoAccountEndpointAndKey => string.IsNullOrWhiteSpace(AccountEndpoint) && string.IsNullOrWhiteSpace(AccountKey);
    public bool NoDatabaseNameAndContainerName => string.IsNullOrWhiteSpace(DatabaseName) || string.IsNullOrWhiteSpace(ContainerName);
}
