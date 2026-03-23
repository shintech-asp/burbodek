namespace burbodek.Models
{
    public class Interview
    {
        public int Id { get; set; }
        public int UsersId { get; set; }
        public Users Users { get; set; }
        public int JobsId { get; set; }
        public Jobs Jobs { get; set; }
        public DateOnly InterviewDate { get; set; }
        public TimeOnly InterviewTime { get; set; }
        public string InterviewFormat { get; set; }
        public string InterviewerName { get; set; }
        public string InterviewLocation { get; set; }
        public string Status { get; set; }
    }
}
