namespace burbodek.Models
{
    public class EmailThread
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public Users? Creator { get; set; }
        public ICollection<Email>? Emails { get; set; }
        public bool IsTrashed { get; set; }
    }
}
