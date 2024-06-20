namespace TodoApp.Mongo.Endpoints.TodoEndpoints.Models;

public record TodoChangeTitleRequest(string Title, long Version);