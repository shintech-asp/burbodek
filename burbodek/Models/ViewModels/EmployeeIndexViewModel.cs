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
        public List<JobRequiredBadge> JobRequiredBadge { get; set; } = new List<JobRequiredBadge>();
        public DateTime CreatedAt { get; set; }
    }
    public class TrainingItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string TrainingDescription { get; set; }
        public string EmployerAddress { get; set; }
        public decimal Price { get; set; }
        public string ModeOfPayment { get; set; }
        public string PaymentOption { get; set; }
        public bool AlreadyApplied { get; set; }
        public string TrainingBadge { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class JobListViewModel
    {
        public List<JobItemViewModel> Jobs { get; set; } = new List<JobItemViewModel>();
        public List<TrainingItemViewModel> Trainings { get; set; } = new List<TrainingItemViewModel>();

        // Pagination
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        // For search persistence
        public string Keyword { get; set; }
        public string Location { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string Filter { get; set; }
    }


}
