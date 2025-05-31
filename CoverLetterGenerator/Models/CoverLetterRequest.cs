using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;  // <-- needed for IFormFile

namespace CoverLetterApp.Models
{
    public class CoverLetterRequest
    {
        [Required]
        [Display(Name = "Job Description File")]
        public IFormFile JobDescriptionFile { get; set; }

        [Required]
        [Display(Name = "Curriculum Vitae (CV) File")]
        public IFormFile CVFile { get; set; }
    }
}
