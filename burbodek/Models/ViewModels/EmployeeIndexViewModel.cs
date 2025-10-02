namespace burbodek.Models.ViewModels
{
    public class JobItemViewModel
    {
        public int Id { get; set; }
        public string JobTitle { get; set; }
        public string JobDescription { get; set; }
        public string EmployerAddress { get; set; }
        public int SalaryMin { get; set; }
        public int SalaryMax { get; set; }
        public bool AlreadyApplied { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class JobListViewModel
    {
        public List<JobItemViewModel> Jobs { get; set; } = new List<JobItemViewModel>();

        // Pagination
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        // For search persistence
        public string Keyword { get; set; }
        public string Location { get; set; }
    }

}
