﻿using E_Commerce.Application.Common.Exceptions;
using E_Commerce.Application.TodoLists.Commands.CreateTodoList;
using E_Commerce.Application.TodoLists.Commands.DeleteTodoList;
using E_Commerce.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

namespace E_Commerce.Application.IntegrationTests.TodoLists.Commands;

using static Testing;

public class DeleteTodoListTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidTodoListId()
    {
        var command = new DeleteTodoListCommand(99);
        await FluentActions.Invoking(() => SendAsync(command)).Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldDeleteTodoList()
    {
        var listId = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        await SendAsync(new DeleteTodoListCommand(listId));

        var list = await FindAsync<TodoList>(listId);

        list.Should().BeNull();
    }
}
