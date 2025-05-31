namespace CoverLetterApp.Models
{
    public class CoverLetterResponse
    {
        public string CoverLetter { get; set; }
        public string CoverLetterBody { get; set; }

        // Parsed from the CV:
        public string Name { get; set; }
        public string Title { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
    }
}