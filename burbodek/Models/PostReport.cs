namespace burbodek.Models
{
    public class PostReport
    {
        public int Id { get; set; }
        public int? JobsId { get; set; }
        public Jobs Jobs { get; set; }
        public int? TrainingId { get; set; }
        public Training Training { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }
        public int UsersId { get; set; }
        public Users Users { get; set; }
        public bool? isDeleted { get; set; }
        public bool? isRetained { get; set; }
        public DateTime DateReported { get; set; } = DateTime.Now;
    }
}
