using System.ComponentModel.DataAnnotations;
using TodoList.Api.Models.Entities;

namespace TodoList.Api.Models.Dtos;

public record TodoDto(
    int Id,
    string Title,
    string? Description,
    bool IsCompleted,
    TodoPriority Priority,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class CreateTodoRequest
{
    [Required, MaxLength(200)]
    public required string Title { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public TodoPriority Priority { get; set; } = TodoPriority.Medium;

    public DateTime? DueDate { get; set; }
}

public class UpdateTodoRequest
{
    [Required, MaxLength(200)]
    public required string Title { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public TodoPriority Priority { get; set; } = TodoPriority.Medium;

    public DateTime? DueDate { get; set; }

    public bool IsCompleted { get; set; }
}
