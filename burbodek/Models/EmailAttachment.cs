namespace burbodek.Models
{
    public class EmailAttachment
    {
        public int Id { get; set; }
        public int EmailID { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty; // Stored path or URL
        public long FileSize { get; set; }

        // Navigation
        public Email? Email { get; set; }
    }

}
