namespace burbodek.Models
{
    public class Images
    {
        public int Id { get; set; }
        public int UsersId { get; set; }
        public Users Users { get; set; }
        public byte[] Image { get; set; }
        public string ImageDetails { get; set; }
        public DateTime? isArchive { get; set; }
    }
}
