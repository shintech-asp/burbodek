namespace burbodek.Models.DTO
{
    public class ComposeEmailDto
    {
        public int? EmailId { get; set; } // null = new, otherwise = edit draft
        public string Subject { get; set; }
        public string Body { get; set; }

        public string ToRecipients { get; set; }  // comma-separated emails
        public string? CcRecipients { get; set; }
        public string? BccRecipients { get; set; }

        public IFormFile[] Files { get; set; } = Array.Empty<IFormFile>();
        public int[] RemovedAttachmentIds { get; set; } = Array.Empty<int>();
    }
}
