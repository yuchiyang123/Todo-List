using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoList.Api.Auth;
using TodoList.Api.Models.Dtos;
using TodoList.Api.Services;

namespace TodoList.Api.Controllers;

[ApiController]
[Route("api/todos")]
[Authorize]
public class TodosController(ITodoService todoService) : ControllerBase
{
    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("Token 缺少使用者識別資訊");

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TodoDto>>> GetAll([FromQuery] bool? completed, [FromQuery] string? keyword)
    {
        var todos = await todoService.GetAllAsync(UserId, completed, keyword);
        return Ok(todos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TodoDto>> GetById(int id)
    {
        var todo = await todoService.GetByIdAsync(UserId, id);
        return todo is null ? NotFound() : Ok(todo);
    }

    [HttpPost]
    [ServiceFilter(typeof(CsrfValidationFilter))]
    public async Task<ActionResult<TodoDto>> Create([FromBody] CreateTodoRequest request)
    {
        var todo = await todoService.CreateAsync(UserId, request);
        return CreatedAtAction(nameof(GetById), new { id = todo.Id }, todo);
    }

    [HttpPut("{id:int}")]
    [ServiceFilter(typeof(CsrfValidationFilter))]
    public async Task<ActionResult<TodoDto>> Update(int id, [FromBody] UpdateTodoRequest request)
    {
        var todo = await todoService.UpdateAsync(UserId, id, request);
        return todo is null ? NotFound() : Ok(todo);
    }

    [HttpPatch("{id:int}/complete")]
    [ServiceFilter(typeof(CsrfValidationFilter))]
    public async Task<ActionResult<TodoDto>> ToggleComplete(int id)
    {
        var todo = await todoService.ToggleCompleteAsync(UserId, id);
        return todo is null ? NotFound() : Ok(todo);
    }

    [HttpDelete("{id:int}")]
    [ServiceFilter(typeof(CsrfValidationFilter))]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await todoService.DeleteAsync(UserId, id);
        return deleted ? NoContent() : NotFound();
    }
}
