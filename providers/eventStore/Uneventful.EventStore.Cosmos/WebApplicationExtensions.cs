using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace Uneventful.EventStore.Cosmos;

public static class WebApplicationExtensions {
    
    public static CosmosClient GetEventStoreCosmosClient(this WebApplication webApplication) {
        var eventStore = (CosmosEventStore)webApplication.Services.GetRequiredService<IEventStore>();
        return eventStore.GetCosmosEventStore().Client;
    }
}