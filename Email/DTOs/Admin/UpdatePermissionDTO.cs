namespace Email.DTOs.Admin
{
    public class UpdatePermissionDTO
    {
        public int AccountId { get; set; }
        public int TaskId { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanComment { get; set; }
    }
}
