using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Cosmos.Domain.Aggregates;
using TodoApp.Cosmos.Endpoints.TodoEndpoints.Models;
using TodoApp.Cosmos.EventListener;
using TodoApp.Cosmos.State;
using TodoApp.Cosmos.Utilities;
using Uneventful.EventStore.InMemory;
using Uneventful.Repository;
using Uneventful.Snapshot.InMemory;

namespace TodoApp.Cosmos.Tests.EndpointTests;

public class TodoEndpointTests {
    private readonly WebApplicationFactory<Program> _factory;
    private readonly AggregateRepository _repository;
    private readonly AppState _appState;

    public TodoEndpointTests() {
        var eventStore = new InMemoryEventStore();
        var snapshotStore  = new InMemorySnapshotStore();

        _repository = new AggregateRepository(eventStore, snapshotStore);
        _appState = new AppState();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(config => {
                config.ConfigureServices(services => {
                     services.Remove(services.First(x => x.ServiceType == typeof(AggregateRepository)));
                     services.AddSingleton(_repository);
                     
                     services.Remove(services.First(x => x.ServiceType == typeof(AppState)));
                     services.AddSingleton(_appState);

                     services.Remove(services.First(x => x.ServiceType == typeof(SetupCosmosDb)));
                     services.Remove(services.First(x => x.ServiceType == typeof(TodoEventListener)));
                });
            });
    }
    
    [Fact]
    public async Task GetEmptyTodos_ReturnsOkWithEmptyList() {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/todo");
        response.EnsureSuccessStatusCode();
        
        var todos = await response.Content.ReadFromJsonAsync<TodoItem[]>();
        Assert.NotNull(todos);
        Assert.Empty(todos);
    }
    
    [Fact]
    public async Task GetTodos_ReturnsOk() {
        var id = Guid.NewGuid();
        _appState.Add(new TodoItem(id, "Test Todo", 1));
        
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/todo");
        response.EnsureSuccessStatusCode();
        
        var todos = await response.Content.ReadFromJsonAsync<TodoItem[]>();
        Assert.NotNull(todos);
        Assert.Single(todos);

        var todo = todos.Single();
        Assert.Equal("Test Todo", todo.Title);
        Assert.Equal(1, todo.Version);
        Assert.Equal(id, todo.Id);
    }
    
    [Fact]
    public async Task PostTodo_ReturnsCreated() {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/todo", new { title = "Test Todo" });
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var responseBody = await response.Content.ReadFromJsonAsync<CommandResult>();
        Assert.NotNull(responseBody);
        Assert.NotEqual(Guid.Empty.ToString(), responseBody.Id);
        Assert.Equal(1, responseBody.Version);

        var todo = await _repository.LoadAsync<Todo>(Todo.GetStreamId(Guid.Parse(responseBody.Id)));
        Assert.NotNull(todo);
        Assert.Equal("Test Todo", todo.Title);
        Assert.Equal(1, todo.Version);
        Assert.Equal(Guid.Parse(responseBody.Id), todo.Id);
    }
    
    [Fact]
    public async Task PutTodoTitle_ReturnsCreated() {
        var todo = Todo.Create(Guid.NewGuid(), "Test Todo");
        await _repository.SaveAsync(todo);
        
        var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync($"/api/todo/{todo.Id}/title", new { title = "Updated Test Todo", version = 1 });
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseBody = await response.Content.ReadFromJsonAsync<CommandResult>();
        Assert.NotNull(responseBody);
        Assert.NotEqual(Guid.Empty.ToString(), responseBody.Id);
        Assert.Equal(2, responseBody.Version);

        todo = await _repository.LoadAsync<Todo>(Todo.GetStreamId(Guid.Parse(responseBody.Id)));
        Assert.NotNull(todo);
        Assert.Equal("Updated Test Todo", todo.Title);
        Assert.Equal(2, todo.Version);
        Assert.Equal(Guid.Parse(responseBody.Id), todo.Id);
    }
    
    [Fact]
    public async Task PutTodoComplete_ReturnsOk() {
        var todo = Todo.Create(Guid.NewGuid(), "Test Todo");
        await _repository.SaveAsync(todo);
        
        var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync($"/api/todo/{todo.Id}/complete", new { });
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseBody = await response.Content.ReadFromJsonAsync<CommandResult>();
        Assert.NotNull(responseBody);
        Assert.NotEqual(Guid.Empty.ToString(), responseBody.Id);
        Assert.Equal(2, responseBody.Version);

        todo = await _repository.LoadAsync<Todo>(Todo.GetStreamId(Guid.Parse(responseBody.Id)));
        Assert.NotNull(todo);
        Assert.True(todo.IsCompleted);
        Assert.Equal(2, todo.Version);
        Assert.Equal(Guid.Parse(responseBody.Id), todo.Id);
    }
    
    [Fact]
    public async Task PutTodoUncomplete_ReturnsOk() {
        var todo = Todo.Create(Guid.NewGuid(), "Test Todo");
        todo.Complete();
        await _repository.SaveAsync(todo);
        
        var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync($"/api/todo/{todo.Id}/uncomplete", new { });
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseBody = await response.Content.ReadFromJsonAsync<CommandResult>();
        Assert.NotNull(responseBody);
        Assert.NotEqual(Guid.Empty.ToString(), responseBody.Id);
        Assert.Equal(3, responseBody.Version);

        todo = await _repository.LoadAsync<Todo>(Todo.GetStreamId(Guid.Parse(responseBody.Id)));
        Assert.NotNull(todo);
        Assert.False(todo.IsCompleted);
        Assert.Equal(3, todo.Version);
        Assert.Equal(Guid.Parse(responseBody.Id), todo.Id);
    }

    [Fact]
    public async Task DeleteTodo_ReturnsOk() {
        var todo = Todo.Create(Guid.NewGuid(), "Test Todo");
        await _repository.SaveAsync(todo);
        
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync($"/api/todo/{todo.Id}");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseBody = await response.Content.ReadFromJsonAsync<CommandResult>();
        Assert.NotNull(responseBody);
        Assert.NotEqual(Guid.Empty.ToString(), responseBody.Id);
        Assert.Equal(2, responseBody.Version);

        todo = await _repository.LoadAsync<Todo>(Todo.GetStreamId(Guid.Parse(responseBody.Id)));
        Assert.NotNull(todo);
        Assert.True(todo.IsRemoved);
        Assert.Equal("Test Todo", todo.Title);
        Assert.Equal(2, todo.Version);
        Assert.Equal(Guid.Parse(responseBody.Id), todo.Id);
    }
}