using System.ComponentModel.DataAnnotations;
namespace Email.Models
{
    public class TaskAssignment
    {
        [Key]
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int AccountId { get; set; }
        public int AssignedById { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
