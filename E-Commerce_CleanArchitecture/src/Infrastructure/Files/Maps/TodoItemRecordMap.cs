using System.Globalization;
using E_Commerce.Application.TodoLists.Queries.ExportTodos;
using CsvHelper.Configuration;

namespace E_Commerce.Infrastructure.Files.Maps;

public class TodoItemRecordMap : ClassMap<TodoItemRecord>
{
    public TodoItemRecordMap()
    {
        AutoMap(CultureInfo.InvariantCulture);

        Map(m => m.Done).Convert(c => c.Value.Done ? "Yes" : "No");
    }
}
