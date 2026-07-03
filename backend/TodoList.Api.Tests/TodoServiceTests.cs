using Microsoft.EntityFrameworkCore;
using TodoList.Api.Data;
using TodoList.Api.Models.Dtos;
using TodoList.Api.Models.Entities;
using TodoList.Api.Services;
using Xunit;

namespace TodoList.Api.Tests;

public class TodoServiceTests
{
    private static TodoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TodoDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_PersistsTodoForOwner()
    {
        await using var db = CreateContext();
        var service = new TodoService(db);

        var created = await service.CreateAsync("user-1", new CreateTodoRequest { Title = "Buy milk" });

        Assert.Equal("Buy milk", created.Title);
        Assert.False(created.IsCompleted);

        var all = await service.GetAllAsync("user-1", null, null);
        Assert.Single(all);
    }

    [Fact]
    public async Task GetAllAsync_DoesNotReturnOtherUsersTodos()
    {
        await using var db = CreateContext();
        var service = new TodoService(db);

        await service.CreateAsync("user-1", new CreateTodoRequest { Title = "User 1 todo" });
        await service.CreateAsync("user-2", new CreateTodoRequest { Title = "User 2 todo" });

        var user1Todos = await service.GetAllAsync("user-1", null, null);

        Assert.Single(user1Todos);
        Assert.Equal("User 1 todo", user1Todos[0].Title);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNullWhenTodoBelongsToAnotherUser()
    {
        await using var db = CreateContext();
        var service = new TodoService(db);

        var created = await service.CreateAsync("user-1", new CreateTodoRequest { Title = "Original" });

        var result = await service.UpdateAsync("user-2", created.Id, new UpdateTodoRequest
        {
            Title = "Hijacked",
            IsCompleted = true
        });

        Assert.Null(result);

        var stillOriginal = await service.GetByIdAsync("user-1", created.Id);
        Assert.Equal("Original", stillOriginal!.Title);
    }

    [Fact]
    public async Task ToggleCompleteAsync_FlipsCompletionState()
    {
        await using var db = CreateContext();
        var service = new TodoService(db);

        var created = await service.CreateAsync("user-1", new CreateTodoRequest { Title = "Task" });

        var toggled = await service.ToggleCompleteAsync("user-1", created.Id);
        Assert.True(toggled!.IsCompleted);

        var toggledAgain = await service.ToggleCompleteAsync("user-1", created.Id);
        Assert.False(toggledAgain!.IsCompleted);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenTodoBelongsToAnotherUser()
    {
        await using var db = CreateContext();
        var service = new TodoService(db);

        var created = await service.CreateAsync("user-1", new CreateTodoRequest { Title = "Task" });

        var deletedByWrongUser = await service.DeleteAsync("user-2", created.Id);
        Assert.False(deletedByWrongUser);

        var deletedByOwner = await service.DeleteAsync("user-1", created.Id);
        Assert.True(deletedByOwner);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByCompletedAndKeyword()
    {
        await using var db = CreateContext();
        var service = new TodoService(db);

        var a = await service.CreateAsync("user-1", new CreateTodoRequest { Title = "Write report" });
        await service.CreateAsync("user-1", new CreateTodoRequest { Title = "Clean house" });
        await service.ToggleCompleteAsync("user-1", a.Id);

        var completed = await service.GetAllAsync("user-1", true, null);
        Assert.Single(completed);
        Assert.Equal("Write report", completed[0].Title);

        var keywordMatch = await service.GetAllAsync("user-1", null, "clean");
        Assert.Single(keywordMatch);
        Assert.Equal("Clean house", keywordMatch[0].Title);
    }
}
