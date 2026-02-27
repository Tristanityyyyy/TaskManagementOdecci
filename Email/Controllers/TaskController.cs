using Email.Data;
using Email.DTOs.Task;
using Email.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Email.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly AccountDbContext _context;

        public TaskController(AccountDbContext context)
        {
            _context = context;
        }

        // GET all tasks
        [HttpGet("GetAllTasks")]
        public async Task<IActionResult> GetAllTasks()
        {
            try
            {
                var tasks = await _context.Tasks
                    .Where(t => !t.IsDeleted)
                    .Select(t => new TaskResponseDTO
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        Status = t.Status,
                        Priority = t.Priority,
                        ReporterId = t.ReporterId,
                        DueDate = t.DueDate,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        AssigneeIds = t.Assignments.Select(a => a.AccountId).ToList()
                    })
                    .ToListAsync();

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET task by id
        [HttpGet("GetTaskById/{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            try
            {
                var task = await _context.Tasks
                    .Where(t => t.Id == id && !t.IsDeleted)
                    .Select(t => new TaskResponseDTO
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        Status = t.Status,
                        Priority = t.Priority,
                        ReporterId = t.ReporterId,
                        DueDate = t.DueDate,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        AssigneeIds = t.Assignments.Select(a => a.AccountId).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (task == null)
                    return NotFound("Task not found.");

                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST create task
        [HttpPost("CreateTask")]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDTO dto, [FromQuery] int adminId)
        {
            try
            {
                var task = new TaskItem
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    Priority = dto.Priority,
                    DueDate = dto.DueDate,
                    ReporterId = adminId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                // Assign users if provided
                if (dto.AssigneeIds.Any())
                {
                    foreach (var accountId in dto.AssigneeIds)
                    {
                        _context.TaskAssignments.Add(new TaskAssignment
                        {
                            TaskId = task.Id,
                            AccountId = accountId,
                            AssignedById = adminId,
                            AssignedAt = DateTime.UtcNow
                        });
                    }

                    await _context.SaveChangesAsync();
                }

                // Log it
                _context.TimeLogs.Add(new TimeLog
                {
                    TaskId = task.Id,
                    AccountId = adminId,
                    Action = "Created",
                    NewValue = task.Title,
                    Note = "Task created by admin"
                });

                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PATCH update task
        [HttpPatch("UpdateTask/{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDTO dto, [FromQuery] int adminId)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);

                if (task == null || task.IsDeleted)
                    return NotFound("Task not found.");

                var changes = new List<string>();

                if (dto.Title != null && dto.Title != task.Title)
                {
                    changes.Add($"Title: {task.Title} → {dto.Title}");
                    task.Title = dto.Title;
                }
                if (dto.Description != null && dto.Description != task.Description)
                {
                    changes.Add($"Description updated");
                    task.Description = dto.Description;
                }
                if (dto.Status != null && dto.Status != task.Status)
                {
                    changes.Add($"Status: {task.Status} → {dto.Status}");
                    task.Status = dto.Status;
                }
                if (dto.Priority != null && dto.Priority != task.Priority)
                {
                    changes.Add($"Priority: {task.Priority} → {dto.Priority}");
                    task.Priority = dto.Priority;
                }
                if (dto.DueDate != null && dto.DueDate != task.DueDate)
                {
                    changes.Add($"DueDate: {task.DueDate} → {dto.DueDate}");
                    task.DueDate = dto.DueDate;
                }

                task.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Log changes
                if (changes.Any())
                {
                    _context.TimeLogs.Add(new TimeLog
                    {
                        TaskId = task.Id,
                        AccountId = adminId,
                        Action = "Updated",
                        NewValue = string.Join(", ", changes),
                        Note = "Task updated by admin"
                    });

                    await _context.SaveChangesAsync();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE task (soft delete)
        [HttpDelete("DeleteTask/{id}")]
        public async Task<IActionResult> DeleteTask(int id, [FromQuery] int adminId)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);

                if (task == null || task.IsDeleted)
                    return NotFound("Task not found.");

                task.IsDeleted = true;
                task.UpdatedAt = DateTime.UtcNow;

                // Log it
                _context.TimeLogs.Add(new TimeLog
                {
                    TaskId = task.Id,
                    AccountId = adminId,
                    Action = "Deleted",
                    OldValue = task.Title,
                    Note = "Task deleted by admin"
                });

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PATCH assign task
        [HttpPatch("AssignTask/{id}")]
        public async Task<IActionResult> AssignTask(int id, [FromBody] AssignTaskDTO dto)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);

                if (task == null || task.IsDeleted)
                    return NotFound("Task not found.");

                // Remove existing assignments
                var existing = _context.TaskAssignments.Where(a => a.TaskId == id);
                _context.TaskAssignments.RemoveRange(existing);

                // Add new assignments
                foreach (var accountId in dto.AssigneeIds)
                {
                    _context.TaskAssignments.Add(new TaskAssignment
                    {
                        TaskId = id,
                        AccountId = accountId,
                        AssignedById = dto.AssignedById,
                        AssignedAt = DateTime.UtcNow
                    });
                }

                task.UpdatedAt = DateTime.UtcNow;

                // Log it
                _context.TimeLogs.Add(new TimeLog
                {
                    TaskId = id,
                    AccountId = dto.AssignedById,
                    Action = "Assigned",
                    NewValue = string.Join(", ", dto.AssigneeIds),
                    Note = "Task assigned by admin"
                });

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}