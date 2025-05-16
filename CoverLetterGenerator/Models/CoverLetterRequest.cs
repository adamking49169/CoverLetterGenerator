using System.ComponentModel.DataAnnotations;

namespace CoverLetterApp.Models
{
    public class CoverLetterRequest
    {
        [Required, Display(Name = "Job Description")]
        public string JobDescription { get; set; }

        [Required, Display(Name = "Curriculum Vitae (CV)")]
        public string CVText { get; set; }
    }
}
