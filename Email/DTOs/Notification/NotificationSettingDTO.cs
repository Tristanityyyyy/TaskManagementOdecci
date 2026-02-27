namespace Email.DTOs.Notification
{
    public class NotificationSettingDTO
    {
        public int AccountId { get; set; }
        public bool EmailOnAssign { get; set; }
        public bool EmailOnStatusChange { get; set; }
        public bool EmailOnDeadlineReminder { get; set; }
        public int DeadlineReminderDaysBefore { get; set; } = 1;
    }
}
