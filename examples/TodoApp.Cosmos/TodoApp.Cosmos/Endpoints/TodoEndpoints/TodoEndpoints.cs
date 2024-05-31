using TodoApp.Cosmos.Domain.Aggregates;
using TodoApp.Cosmos.Endpoints.TodoEndpoints.Models;
using TodoApp.Cosmos.State;
using Uneventful.Repository;

namespace TodoApp.Cosmos.Endpoints.TodoEndpoints;

internal static class TodoEndpoints {
    public static IEndpointRouteBuilder UseTodoEndpoints(this IEndpointRouteBuilder endpoints) {

        endpoints.MapGet("/api/todo", (AppState appState) => appState.TodoItems);
        endpoints.MapPost("/api/todo", async (AggregateRepository repository, TodoCreateRequest req, CancellationToken cancellationToken) => {
            var todo = Todo.Create(Guid.NewGuid(), req.Title);
            await repository.SaveAsync(todo, cancellationToken: cancellationToken);
            return Results.Created($"/api/todo/{todo.Id}", new CommandResult(todo.Id.ToString(), todo.Version, new Dictionary<string, object>() {
                ["CreatedOn"] = todo.CreatedOn
            }));
        });
        
        endpoints.MapGet("/api/todo/{id}", (AppState appState, Guid id) => appState.Get(id));
        
        endpoints.MapPut("/api/todo/{id}/title", async (
            AggregateRepository repository, 
            Guid id,
            TodoChangeTitleRequest req,
            CancellationToken cancellationToken
        ) => {
            var todo = await repository.LoadAsync<Todo>(Todo.GetStreamId(id), cancellationToken);
            if (todo == null) {
                return Results.NotFound("Item is not found");
            }
            
            if (todo.Version != req.Version) {
                return Results.Conflict("Version mismatch");
            }
            
            todo.ChangeTitle(req.Title);
            await repository.SaveAsync(todo, cancellationToken: cancellationToken);
            
            return Results.Ok(new CommandResult(todo.Id.ToString(), todo.Version));
        });
        
        endpoints.MapDelete(
            "/api/todo/{id}", 
            async (AggregateRepository repository, Guid id, CancellationToken cancellationToken) => {
                var todo = await repository.LoadAsync<Todo>(Todo.GetStreamId(id), cancellationToken);
                if (todo == null) {
                    return Results.NotFound("Item is not found");
                }
                todo.Remove();
                await repository.SaveAsync(todo, cancellationToken);
                
                return Results.Ok(new CommandResult(todo.Id.ToString(), todo.Version));
            }
        );
        
        endpoints.MapPut(
            "/api/todo/{id}/complete", 
            async (AggregateRepository repository, Guid id, CancellationToken cancellationToken) => {
                var todo = await repository.LoadAsync<Todo>(Todo.GetStreamId(id), cancellationToken);
                if (todo == null) {
                    return Results.NotFound("Item is not found");
                }
                todo.Complete();
                await repository.SaveAsync(todo, cancellationToken);
                return Results.Ok(new CommandResult(todo.Id.ToString(), todo.Version));
            }
        );

        endpoints.MapPut(
            "/api/todo/{id}/uncomplete",
            async (AggregateRepository repository, Guid id, CancellationToken cancellationToken) => {
                var todo = await repository.LoadAsync<Todo>(Todo.GetStreamId(id), cancellationToken);
                if (todo == null) {
                    return Results.NotFound("Item is not found");
                }

                todo.UnComplete();
                await repository.SaveAsync(todo, cancellationToken);
                return Results.Ok(new CommandResult(todo.Id.ToString(), todo.Version));
            }
        );

        return endpoints;
    }
}