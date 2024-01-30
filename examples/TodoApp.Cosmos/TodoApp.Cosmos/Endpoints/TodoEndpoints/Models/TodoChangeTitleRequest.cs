namespace TodoApp.Cosmos.Endpoints.TodoEndpoints.Models;

public record TodoChangeTitleRequest(string Title, long Version);