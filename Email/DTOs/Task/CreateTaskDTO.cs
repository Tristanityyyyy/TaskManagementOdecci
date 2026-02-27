namespace Email.DTOs.Task
{
    public class CreateTaskDTO
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Priority { get; set; }
        public int? StoryPoints { get; set; }       // 👈 add
        public int ProjectId { get; set; }          // 👈 add
        public int? ParentTaskId { get; set; }      // 👈 add
        public DateTime? DueDate { get; set; }

        public List<int> AssigneeIds { get; set; } = new List<int>(); // multiple assignees

    }
}
