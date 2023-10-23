using E_Commerce.Application.TodoLists.Queries.ExportTodos;

namespace E_Commerce.Application.Common.Interfaces;

public interface ICsvFileBuilder
{
    byte[] BuildTodoItemsFile(IEnumerable<TodoItemRecord> records);
}
