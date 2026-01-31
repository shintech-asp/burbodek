namespace burbodek.Models
{
    public class EmployeeDetails
    {
        public int Id { get; set; }
        public int UsersId { get; set; }
        public Users Users { get; set; }
        public string Firstname { get; set; }
        public string Middlename { get; set; }
        public string Lastname { get; set; }
        public DateOnly Birthday { get; set; }
        public string Nationality { get; set; }
        public string MobileNumber { get; set; }
    }
}
