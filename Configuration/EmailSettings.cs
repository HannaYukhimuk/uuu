namespace UserManagementApp.Configuration
{
    public class EmailSettings
    {
        public required string SmtpServer { get; set; }
        public required int SmtpPort { get; set; }
        public required string SmtpUser { get; set; }
        public required string SmtpPass { get; set; }
        public required string SenderName { get; set; }
        public required string SenderEmail { get; set; }
    }
}
