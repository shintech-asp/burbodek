namespace burbodek.Models
{
    public class Faq
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool isActive { get; set; }
    }

    public class FaqTitle
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }
}
