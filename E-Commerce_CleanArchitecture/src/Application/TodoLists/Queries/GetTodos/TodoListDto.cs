using E_Commerce.Application.Common.Mappings;
using E_Commerce.Domain.Entities;

namespace E_Commerce.Application.TodoLists.Queries.GetTodos;

public class TodoListDto : IMapFrom<TodoList>
{
    public TodoListDto()
    {
        Items = Array.Empty<TodoItemDto>();
    }

    public int Id { get; init; }

    public string? Title { get; init; }

    public string? Colour { get; init; }

    public IReadOnlyCollection<TodoItemDto> Items { get; init; }
}
