namespace burbodek.Models
{
    public class Email
    {
        public int Id { get; set; }
        public int ThreadID { get; set; }
        public int SenderID { get; set; }
        public string Body { get; set; } = string.Empty; // Quill HTML
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsDraft { get; set; } = false; // Draft flag
        public bool IsTrashed { get; set; } = false; // Trash flag
        public bool IsRead { get; set; } = false; // Recipient’s view
        public bool IsStarred { get; set; } = false; // Marked email

        // Navigation
        public EmailThread? Thread { get; set; }
        public Users? Sender { get; set; }
        public ICollection<EmailRecipient>? Recipients { get; set; }
        public ICollection<EmailAttachment>? Attachments { get; set; }
    }
}
