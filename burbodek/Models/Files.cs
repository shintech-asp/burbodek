namespace burbodek.Models
{
    public class Files
    {
        public int Id { get; set; }
        public int UsersId { get; set; }
        public Users Users { get; set; }
        public byte[] File { get; set; }
        public string ImageDetails { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public DateTime? isArchive { get; set; }
    }
}
