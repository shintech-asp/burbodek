namespace burbodek.Models
{
    public class JobApplication
    {
        public int Id { get; set; }
        public int JobsId { get; set; }
        public Jobs Jobs { get; set; }
        public int AppliedBy { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MobileNo { get; set; }
        public int Age { get; set; }
        public string City { get; set; }
        public int ExpectedSalary { get; set; }
        public DateTime StartDate { get; set; }
        public string Experience { get; set; }
        public string ApplicationLetter { get; set; }
        public string CV { get; set; }
        public string Status { get; set; } = "Applied";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
