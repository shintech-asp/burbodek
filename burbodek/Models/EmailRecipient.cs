namespace burbodek.Models
{
    public class EmailRecipient
    {
        public int Id { get; set; }
        public int EmailID { get; set; }
        public int RecipientID { get; set; }
        public RecipientType RecipientType { get; set; } // Enum: TO, CC, BCC
        public bool IsRead { get; set; } = false;
        public bool IsTrashed { get; set; } = false;
        public bool IsStarred { get; set; } = false;

        // Navigation
        public Email? Email { get; set; }
        public Users? Recipient { get; set; }
    }

    public enum RecipientType
    {
        TO,
        CC,
        BCC
    }

}
