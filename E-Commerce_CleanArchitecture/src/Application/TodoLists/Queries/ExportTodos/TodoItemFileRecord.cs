using E_Commerce.Application.Common.Mappings;
using E_Commerce.Domain.Entities;

namespace E_Commerce.Application.TodoLists.Queries.ExportTodos;

public class TodoItemRecord : IMapFrom<TodoItem>
{
    public string? Title { get; init; }

    public bool Done { get; init; }
}
