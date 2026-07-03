using Microsoft.EntityFrameworkCore;
using TodoList.Api.Data;
using TodoList.Api.Models.Dtos;
using TodoList.Api.Models.Entities;

namespace TodoList.Api.Services;

public class TodoService(TodoDbContext db) : ITodoService
{
    public async Task<IReadOnlyList<TodoDto>> GetAllAsync(string userId, bool? isCompleted, string? keyword)
    {
        var query = db.Todos.Where(t => t.UserId == userId);

        if (isCompleted.HasValue)
        {
            query = query.Where(t => t.IsCompleted == isCompleted.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lowered = keyword.ToLowerInvariant();
            query = query.Where(t => t.Title.ToLower().Contains(lowered) || (t.Description != null && t.Description.ToLower().Contains(lowered)));
        }

        var todos = await query.OrderBy(t => t.IsCompleted).ThenBy(t => t.DueDate).ThenByDescending(t => t.CreatedAt).ToListAsync();
        return todos.Select(ToDto).ToList();
    }

    public async Task<TodoDto?> GetByIdAsync(string userId, int id)
    {
        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        return todo is null ? null : ToDto(todo);
    }

    public async Task<TodoDto> CreateAsync(string userId, CreateTodoRequest request)
    {
        var todo = new TodoItem
        {
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            DueDate = request.DueDate,
        };

        db.Todos.Add(todo);
        await db.SaveChangesAsync();
        return ToDto(todo);
    }

    public async Task<TodoDto?> UpdateAsync(string userId, int id, UpdateTodoRequest request)
    {
        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (todo is null) return null;

        todo.Title = request.Title;
        todo.Description = request.Description;
        todo.Priority = request.Priority;
        todo.DueDate = request.DueDate;
        todo.IsCompleted = request.IsCompleted;
        todo.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return ToDto(todo);
    }

    public async Task<TodoDto?> ToggleCompleteAsync(string userId, int id)
    {
        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (todo is null) return null;

        todo.IsCompleted = !todo.IsCompleted;
        todo.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return ToDto(todo);
    }

    public async Task<bool> DeleteAsync(string userId, int id)
    {
        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (todo is null) return false;

        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return true;
    }

    private static TodoDto ToDto(TodoItem t) => new(
        t.Id, t.Title, t.Description, t.IsCompleted, t.Priority, t.DueDate, t.CreatedAt, t.UpdatedAt);
}
