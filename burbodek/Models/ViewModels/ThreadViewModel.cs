namespace burbodek.Models.ViewModels
{
    public class ThreadViewModel
    {
        public int ThreadID { get; set; }
        public string Subject { get; set; }
        public List<Email> Emails { get; set; }
    }
}
