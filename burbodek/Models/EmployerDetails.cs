namespace burbodek.Models
{
    public class EmployerDetails
    {
        public int Id { get; set; }
        public int UsersId { get; set; }
        public Users Users { get; set; }
        public int? isTrainingCenter { get; set; }
        public int? isEmployer { get; set; }
        public string BusinessName { get; set; }
        public string BusinessDescription { get; set; }
        public string Status { get; set; } = "For Approval";
        public string? RejectionReason { get; set; }
        public string Address { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public int? RegistrationCount { get; set; }
        public bool? isAllowedForResubmission { get; set; }
    }
}
