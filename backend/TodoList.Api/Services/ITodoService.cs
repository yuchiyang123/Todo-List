using TodoList.Api.Models.Dtos;

namespace TodoList.Api.Services;

public interface ITodoService
{
    Task<IReadOnlyList<TodoDto>> GetAllAsync(string userId, bool? isCompleted, string? keyword);
    Task<TodoDto?> GetByIdAsync(string userId, int id);
    Task<TodoDto> CreateAsync(string userId, CreateTodoRequest request);
    Task<TodoDto?> UpdateAsync(string userId, int id, UpdateTodoRequest request);
    Task<TodoDto?> ToggleCompleteAsync(string userId, int id);
    Task<bool> DeleteAsync(string userId, int id);
}
