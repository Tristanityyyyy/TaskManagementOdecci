namespace TaskManagement.DTOs.Project
{
    public class UpdateProjectDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; } // Active, OnHold, Completed, Cancelled
        public int? ProjectManagerId { get; set; }
        public int? ScrumMasterId { get; set; }
        public List<int>? MemberIds { get; set; }
    }
}
