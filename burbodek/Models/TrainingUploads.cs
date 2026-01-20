namespace burbodek.Models
{
    public class TrainingUploads
    {
        public int Id { get; set; }
        public int TrainingId { get; set; }
        public Training Training { get; set; }
        public string Name { get; set; }
        public bool isActive { get; set; }
        public ICollection<ApplicantTrainingUpload> ApplicantTrainingUpload { get; set; } = new List<ApplicantTrainingUpload>();
    }
}
