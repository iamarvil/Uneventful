using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TodoApp.Mongo;
using TodoApp.Mongo.Endpoints.TodoEndpoints;
using TodoApp.Mongo.EventListener;
using TodoApp.Mongo.State;
using Uneventful.EventStore;
using Uneventful.EventStore.Mongo;
using Uneventful.EventStore.Mongo.Serializer;
using Uneventful.Repository;
using ChangeStreamOperationType = MongoDB.Driver.ChangeStreamOperationType;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<AppState>();




builder.Services
    .AddEventStore(
        Constants.DomainName,
        (eventStoreBuilder) => {
            
            eventStoreBuilder.RegisterTodoAppMongoEvents(Constants.DomainName);
            eventStoreBuilder.UseMongoEventStore(
                "mongodb://localhost:27017", 
                "todo-test-2", 
                "todo-events", 
                Constants.DomainName
            );
        }
    )
    .AddAggregateRepository();

builder.Services.AddHostedService<TodoEventListener>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseTodoEndpoints();

await app.RunAsync();
