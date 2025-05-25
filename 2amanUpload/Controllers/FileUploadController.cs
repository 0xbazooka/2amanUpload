using _2amanUpload.Models;
using _2amanUpload.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace _2amanUpload.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly FileValidationService _validationService;
        private readonly ILogger<FileUploadController> _logger;
        private readonly string _storagePath;
        private static readonly List<(string FileId, string FileName, string Title, string Description, string Tags)> _uploads = new List<(string, string, string, string, string)>();

        public FileUploadController(FileValidationService validationService, ILogger<FileUploadController> logger, IConfiguration configuration)
        {
            _validationService = validationService;
            _logger = logger;
            _storagePath = configuration.GetValue<string>("Storage:UploadPath") ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage/Uploads");
            Directory.CreateDirectory(_storagePath); // Ensure directory exists
        }

        public IActionResult Index()
        {
            return View(new FileUploadModel());
        }

        [HttpPost]
        public async Task<IActionResult> Upload(FileUploadModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for file upload in 2amanUpload.");
                ViewBag.ErrorMessage = "Please provide all required fields.";
                return View("Index", model);
            }

            var (isValid, errorMessage) = _validationService.ValidateFile(model.File);
            if (!isValid)
            {
                ViewBag.ErrorMessage = errorMessage;
                return View("Index", model);
            }

            try
            {
                var fileId = Guid.NewGuid().ToString();
                var fileExtension = Path.GetExtension(model.File.FileName).ToLowerInvariant();
                var safeFileName = $"{fileId}{fileExtension}";
                var filePath = Path.Combine(_storagePath, safeFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                _uploads.Add((fileId, safeFileName, model.Title, model.Description, model.Tags));

                _logger.LogInformation($"File uploaded via 2amanUpload: ID={fileId}, Title={model.Title}, Description={model.Description}, Tags={model.Tags ?? "None"}");

                ViewBag.SuccessMessage = $"File uploaded successfully via 2amanUpload. File ID: <a href=\"{Url.Action("Uploads")}\">{fileId}</a>";
                return View("Index", new FileUploadModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file upload in 2amanUpload.");
                ViewBag.ErrorMessage = "An error occurred while uploading the file. Please try again.";
                return View("Index", model);
            }
        }

        public IActionResult Uploads()
        {
            var uploads = new List<(string FileId, string FileName, string Title, string Description, string Tags)>();
            try
            {
                foreach (var upload in _uploads)
                {
                    var filePath = Path.Combine(_storagePath, upload.FileName);
                    if (System.IO.File.Exists(filePath))
                    {
                        uploads.Add(upload);
                    }
                    else
                    {
                        _logger.LogWarning($"File not found on disk: {upload.FileName} in 2amanUpload.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving uploads in 2amanUpload.");
                ViewBag.ErrorMessage = "Error retrieving uploads. Please try again.";
            }

            return View(uploads);
        }

        public IActionResult Download(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                _logger.LogWarning("Download request with null or empty FileId in 2amanUpload.");
                return NotFound("File not found.");
            }

            var upload = _uploads.FirstOrDefault(u => u.FileId == fileId);
            if (upload.FileId == null) // Check if no match was found
            {
                _logger.LogWarning($"Download request for unknown FileId: {fileId} in 2amanUpload.");
                return NotFound("File not found.");
            }

            var filePath = Path.GetFullPath(Path.Combine(_storagePath, upload.FileName)); // Convert to absolute path
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning($"File not found on disk: {upload.FileName} in 2amanUpload.");
                return NotFound("File not found.");
            }

            _logger.LogInformation($"File downloaded via 2amanUpload: ID={fileId}, FileName={upload.FileName}");
            return PhysicalFile(filePath, "application/octet-stream", upload.FileName);
        }
    }
}