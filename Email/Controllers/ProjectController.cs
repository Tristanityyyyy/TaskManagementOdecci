using Email.Data;
using Email.DTOs.Project;
using Email.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Email.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly AccountDbContext _context;

        public ProjectController(AccountDbContext context)
        {
            _context = context;
        }

        [HttpPost("CreateProject")]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectDTO dto, [FromQuery] int creatorId)
        {
            try
            {
                var creator = await _context.Accounts.FindAsync(creatorId);
                if (creator == null)
                    return NotFound("Creator account not found.");

                int projectManagerId;
                int? scrumMasterId;

                if (creator.Role == "Admin")
                {
                    // Admin must select a project manager
                    if (dto.ProjectManagerId == null)
                        return BadRequest("Admin must select a Project Manager.");

                    projectManagerId = dto.ProjectManagerId.Value;
                    scrumMasterId = dto.ScrumMasterId;
                }
                else
                {
                    // User automatically becomes Project Manager
                    projectManagerId = creatorId;

                    // User can also be Scrum Master at the same time
                    if (dto.IsAlsoScrumMaster)
                        scrumMasterId = creatorId;
                    else
                        scrumMasterId = dto.ScrumMasterId;
                }

                // Create the project
                var project = new Project
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    CreatedById = creatorId,
                    ProjectManagerId = projectManagerId,
                    ScrumMasterId = scrumMasterId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                // Add Project Manager as member
                _context.ProjectMembers.Add(new ProjectMember
                {
                    ProjectId = project.Id,
                    AccountId = projectManagerId,
                    Role = "ProjectManager",
                    JoinedAt = DateTime.UtcNow
                });


                // Add Scrum Master as member if different from PM
                if (scrumMasterId != null && scrumMasterId != projectManagerId)
                {
                    _context.ProjectMembers.Add(new ProjectMember
                    {
                        ProjectId = project.Id,
                        AccountId = scrumMasterId.Value,
                        Role = "ScrumMaster",
                        JoinedAt = DateTime.UtcNow
                    });
                }
                // If user is both PM and SM
                else if (scrumMasterId != null && scrumMasterId == projectManagerId)
                {
                    // Update the PM member role to reflect both roles
                    var pmMember = await _context.ProjectMembers
                        .FirstOrDefaultAsync(m => m.ProjectId == project.Id && m.AccountId == projectManagerId);
                    if (pmMember != null)
                        pmMember.Role = "ProjectManager-ScrumMaster";
                }

                // Add Members
                foreach (var memberId in dto.MemberIds)
                {
                    // Skip if already added as PM or SM
                    var alreadyAdded = await _context.ProjectMembers
                        .AnyAsync(m => m.ProjectId == project.Id && m.AccountId == memberId);

                    if (!alreadyAdded)
                    {
                        _context.ProjectMembers.Add(new ProjectMember
                        {
                            ProjectId = project.Id,
                            AccountId = memberId,
                            Role = "Member",
                            JoinedAt = DateTime.UtcNow
                        });
                    }
                }

                // Log it
                _context.TimeLogs.Add(new TimeLog
                {
                    TaskId = null,
                    AccountId = creatorId,
                    Action = "ProjectCreated",
                    NewValue = project.Name,
                    Note = $"Project created by {creator.Role}"
                });

                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, new ProjectResponseDTO
                {
                    Id = project.Id,
                    Name = project.Name,
                    Description = project.Description,
                    Status = project.Status,
                    CreatedById = project.CreatedById,
                    ProjectManagerId = project.ProjectManagerId,
                    ScrumMasterId = project.ScrumMasterId,
                    MemberIds = dto.MemberIds,
                    CreatedAt = project.CreatedAt,
                    UpdatedAt = project.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = ex.Message,
                    inner = ex.InnerException?.Message,
                    inner2 = ex.InnerException?.InnerException?.Message
                });

            }
        }

        // GET all projects
        [HttpGet("GetAllProjects")]
        public async Task<IActionResult> GetAllProjects()
        {
            try
            {
                var projects = await _context.Projects
                    .Where(p => !p.IsDeleted)
                    .Select(p => new ProjectResponseDTO
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Status = p.Status,
                        CreatedById = p.CreatedById,
                        ProjectManagerId = p.ProjectManagerId,
                        ScrumMasterId = p.ScrumMasterId,
                        MemberIds = p.Members.Select(m => m.AccountId).ToList(),
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(projects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET project by id
        [HttpGet("GetProjectById/{id}")]
        public async Task<IActionResult> GetProjectById(int id)
        {
            try
            {
                var project = await _context.Projects
                    .Where(p => p.Id == id && !p.IsDeleted)
                    .Select(p => new ProjectResponseDTO
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Status = p.Status,
                        CreatedById = p.CreatedById,
                        ProjectManagerId = p.ProjectManagerId,
                        ScrumMasterId = p.ScrumMasterId,
                        MemberIds = p.Members.Select(m => m.AccountId).ToList(),
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (project == null)
                    return NotFound("Project not found.");

                return Ok(project);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET my projects
        [HttpGet("GetMyProjects/{accountId}")]
        public async Task<IActionResult> GetMyProjects(int accountId)
        {
            try
            {
                var myProjectIds = await _context.ProjectMembers
                    .Where(m => m.AccountId == accountId)
                    .Select(m => m.ProjectId)
                    .ToListAsync();

                var projects = await _context.Projects
                    .Where(p => myProjectIds.Contains(p.Id) && !p.IsDeleted)
                    .Select(p => new ProjectResponseDTO
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Status = p.Status,
                        CreatedById = p.CreatedById,
                        ProjectManagerId = p.ProjectManagerId,
                        ScrumMasterId = p.ScrumMasterId,
                        MemberIds = p.Members.Select(m => m.AccountId).ToList(),
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(projects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE project (soft delete, admin only)
        [HttpDelete("DeleteProject/{id}")]
        public async Task<IActionResult> DeleteProject(int id, [FromQuery] int adminId)
        {
            try
            {
                var admin = await _context.Accounts.FindAsync(adminId);
                if (admin == null || admin.Role != "Admin")
                    return StatusCode(403, "Access denied. Admins only.");

                var project = await _context.Projects.FindAsync(id);
                if (project == null || project.IsDeleted)
                    return NotFound("Project not found.");

                project.IsDeleted = true;
                project.UpdatedAt = DateTime.UtcNow;

                _context.TimeLogs.Add(new TimeLog
                {
                    TaskId = null,
                    AccountId = adminId,
                    Action = "ProjectDeleted",
                    OldValue = project.Name,
                    Note = "Project deleted by admin"
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