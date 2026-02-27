using System.ComponentModel.DataAnnotations;

namespace Email.Models
{
    public class TaskComment
    {
        [Key]
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int AccountId { get; set; }
        [Required]
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }
}