namespace burbodek.Models
{
    public class EmailTemplate
    {
        public int Id { get; set; }
        public string Category { get; set; } // e.g., "Notification", "Promotion", "Training", "Job Notification"
        public string TypeOfEmail { get; set; } // e.g., "Welcome Email", "Password Reset", "Interview Invitation"
        public string Subject { get; set; } // Subject line of the email
        public string Body { get; set; } // Main content of the email
        public int UsersId { get; set; } //User Id of the creator
        public Users Users { get; set; }
        public string ModifiedAt { get; set; } // Timestamp of the last modification
        public bool isActive { get; set; } // Indicates if the template is active or inactive
    }
}
