namespace burbodek.Models
{
    public class UserProfile
    {
        public int Id { get; set; }
        public int UsersId { get; set; }
        public Users Users { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MobileNo { get; set; }
        public DateOnly? Birthdate { get; set; }
        public string? City { get; set; }
        public string? Picture { get; set; }
        public string? Resume { get; set; }
    }
}
