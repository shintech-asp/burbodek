namespace burbodek.Models.ViewModels
{
    public class InboxViewModel
    {
        public EmailThread Thread { get; set; }
        public Email Email { get; set; }
        public EmailRecipient Recipient { get; set; }
    }
}
