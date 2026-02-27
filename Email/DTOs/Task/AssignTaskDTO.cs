namespace Email.DTOs.Task
{
    public class AssignTaskDTO
    {
        public List<int> AssigneeIds { get; set; } = new List<int>();
        public int AssignedById { get; set; }
    }
}
