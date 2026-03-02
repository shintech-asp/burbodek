using burbodek.Models.DTO;

namespace burbodek.Models.ViewModels
{
    public class ApplicantInfoViewModel
    {
        public Jobs Jobs { get; set; }
        public List<EmailTemplateDTO> EmailTemplate { get; set; }
    }
}
