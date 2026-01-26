using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace burbodek.Models
{
    public class Users
    {
        public int Id { get; set; }
        [Required]
        public string Username { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        public string Role { get; set; }

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$",
        ErrorMessage = "Password must be at least 8 characters and contain a letter, a number, and a symbol.")]
        public string Password { get; set; }
        [NotMapped]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateArchived { get; set; }
        public DateTime? DateModified { get; set; }
        [NotMapped]
        public IFormFile? CapturedIdFile { get; set; }
        public EmployerDetails? EmployerDetails { get; set; }
        public List<Subscription>? Subscription { get; set; }
        public List<JobApplication>? JobApplication { get; set; }
        public List<Files>? Files { get; set; }
        public List<PaymentDetails>? PaymentDetails { get; set; }
        public List<UserBadge>? UserBadge { get; set; }
        public List<Payments>? Payments { get; set; }
        public List<Training>? Training { get; set; }
        public List<Jobs>? Jobs { get; set; }
        public List<EmailTemplate>? EmailTemplate { get; set; }
    }
}
