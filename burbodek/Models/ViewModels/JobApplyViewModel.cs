namespace burbodek.Models.ViewModels
{
    public class JobApplyViewModel
    {
        public Jobs Jobs { get; set; }
        public List<ApplicantTrainingUpload> Uploads { get; set; } = new();
        public JobApplication? UserInfo { get; set; }
        public UserProfile? UserProfile { get; set; }
    }
}
