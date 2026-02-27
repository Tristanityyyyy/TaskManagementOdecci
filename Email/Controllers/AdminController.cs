using Email.Data;
using Email.DTOs.Admin;
using Email.DTOs.Task;
using Email.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Email.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AccountDbContext _context;

        public AdminController(AccountDbContext context)
        {
            _context = context;
        }

        // Force update task status
        [HttpPatch("ForceUpdateStatus/{taskId}")]
        public async Task<IActionResult> ForceUpdateStatus(int taskId, [FromBody] ForceUpdateStatusDTO dto, [FromQuery] int adminId)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId);

                if (task == null || task.IsDeleted)
                    return NotFound("Task not found.");

                var oldStatus = task.Status;
                task.Status = dto.Status;
                task.UpdatedAt = DateTime.UtcNow;

                _context.TimeLogs.Add(new TimeLog
                {
                    TaskId = taskId,
                    AccountId = adminId,
                    Action = "StatusChanged",
                    OldValue = oldStatus,
                    NewValue = dto.Status,
                    Note = dto.Note ?? "Force updated by admin"
                });

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Change task priority
        [HttpPatch("ChangePriority/{taskId}")]
        public async Task<IActionResult> ChangePriority(int taskId, [FromQuery] string priority, [FromQuery] int adminId)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId);

                if (task == null || task.IsDeleted)
                    return NotFound("Task not found.");

                var oldPriority = task.Priority;
                task.Priority = priority;
                task.UpdatedAt = DateTime.UtcNow;

                _context.TimeLogs.Add(new TimeLog
                {
                    TaskId = taskId,
                    AccountId = adminId,
                    Action = "PriorityChanged",
                    OldValue = oldPriority,
                    NewValue = priority,
                    Note = "Priority changed by admin"
                });

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Update task deadline
        [HttpPatch("UpdateDeadline/{taskId}")]
        public async Task<IActionResult> UpdateDeadline(int taskId, [FromQuery] DateTime dueDate, [FromQuery] int adminId)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId);

                if (task == null || task.IsDeleted)
                    return NotFound("Task not found.");

                var oldDueDate = task.DueDate?.ToString() ?? "None";
                task.DueDate = dueDate;
                task.UpdatedAt = DateTime.UtcNow;

                _context.TimeLogs.Add(new TimeLog
                {
                    TaskId = taskId,
                    AccountId = adminId,
                    Action = "DeadlineUpdated",
                    OldValue = oldDueDate,
                    NewValue = dueDate.ToString(),
                    Note = "Deadline updated by admin"
                });

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Reassign task
        [HttpPatch("ReassignTask/{taskId}")]
        public async Task<IActionResult> ReassignTask(int taskId, [FromBody] AssignTaskDTO dto, [FromQuery] int adminId)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId);

                if (task == null || task.IsDeleted)
                    return NotFound("Task not found.");

                // Get old assignees for logging
                var oldAssignees = await _context.TaskAssignments
                    .Where(a => a.TaskId == taskId)
                    .Select(a => a.AccountId)
                    .ToListAsync();

                // Remove existing
                var existing = _context.TaskAssignments.Where(a => a.TaskId == taskId);
                _context.TaskAssignments.RemoveRange(existing);

                // Add new
                foreach (var accountId in dto.AssigneeIds)
                {
                    _context.TaskAssignments.Add(new TaskAssignment
                    {
                        TaskId = taskId,
                        AccountId = accountId,
                        AssignedById = adminId,
                        AssignedAt = DateTime.UtcNow
                    });
                }

                task.UpdatedAt = DateTime.UtcNow;

                _context.TimeLogs.Add(new TimeLog
                {
                    TaskId = taskId,
                    AccountId = adminId,
                    Action = "Reassigned",
                    OldValue = string.Join(", ", oldAssignees),
                    NewValue = string.Join(", ", dto.AssigneeIds),
                    Note = "Task reassigned by admin"
                });

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Update task permission
        [HttpPost("UpdatePermission")]
        public async Task<IActionResult> UpdatePermission([FromBody] UpdatePermissionDTO dto, [FromQuery] int adminId)
        {
            try
            {
                var existing = await _context.TaskPermissions
                    .SingleOrDefaultAsync(p => p.TaskId == dto.TaskId && p.AccountId == dto.AccountId);

                if (existing == null)
                {
                    _context.TaskPermissions.Add(new TaskPermission
                    {
                        TaskId = dto.TaskId,
                        AccountId = dto.AccountId,
                        CanView = dto.CanView,
                        CanEdit = dto.CanEdit,
                        CanDelete = dto.CanDelete,
                        CanComment = dto.CanComment,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.CanView = dto.CanView;
                    existing.CanEdit = dto.CanEdit;
                    existing.CanDelete = dto.CanDelete;
                    existing.CanComment = dto.CanComment;
                    existing.UpdatedAt = DateTime.UtcNow;
                }

                _context.TimeLogs.Add(new TimeLog
                {
                    TaskId = dto.TaskId,
                    AccountId = adminId,
                    Action = "PermissionUpdated",
                    Note = $"Permission updated for account {dto.AccountId} by admin"
                });

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Get task visibility
        [HttpGet("GetTaskPermissions/{taskId}")]
        public async Task<IActionResult> GetTaskPermissions(int taskId)
        {
            try
            {
                var permissions = await _context.TaskPermissions
                    .Where(p => p.TaskId == taskId)
                    .ToListAsync();

                return Ok(permissions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}