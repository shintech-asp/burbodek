namespace burbodek.Models.ViewModels
{
    public class TrainingApplyViewModel
    {
        public Training? Training { get; set; }
        public List<ApplicantTrainingUpload> Uploads { get; set; } = new();
        public TrainingApplication? UserInfo { get; set; }
    }
}
