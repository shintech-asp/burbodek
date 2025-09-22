namespace burbodek.Models
{
    public class EmployerDetails
    {
        public int Id { get; set; }
        public int UsersId { get; set; }
        public Users Users { get; set; }
        public int? isTrainingCenter { get; set; }
        public int? isEmployer { get; set; }
        public int pPlansId { get; set; }
        public string BusinessName { get; set; }
        public string BusinessDescription { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
