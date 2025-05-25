using System.ComponentModel.DataAnnotations;

namespace _2amanUpload.Models
{
    public class FileUploadModel
    {
        [Required(ErrorMessage = "Please select a file to upload.")]
        public IFormFile File { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; }

        public string Tags { get; set; } // Comma-separated string (e.g., "finance,2025")
    }
}