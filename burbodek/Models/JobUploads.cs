namespace burbodek.Models
{
    public class JobUploads
    {
        public int Id { get; set; }
        public int JobsId { get; set; }
        public Jobs Jobs { get; set; }
        public string Name { get; set; }
        public bool isActive { get; set; }
        public ICollection<ApplicantJobUpload> ApplicantJobUpload { get; set; } = new List<ApplicantJobUpload>();
    }
}
