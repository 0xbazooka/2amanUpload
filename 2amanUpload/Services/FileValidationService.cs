using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;

namespace _2amanUpload.Services
{
    public class FileValidationService
    {
        private readonly ILogger<FileValidationService> _logger;
        private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
        private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB

        public FileValidationService(ILogger<FileValidationService> logger)
        {
            _logger = logger;
        }

        public (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("File upload attempt with null or empty file in 2amanUpload.");
                return (false, "File is empty or not provided.");
            }

            if (file.Length > _maxFileSize)
            {
                _logger.LogWarning($"File size exceeds limit: {file.Length} bytes in 2amanUpload.");
                return (false, $"File size exceeds {_maxFileSize / (1024 * 1024)}MB limit.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                _logger.LogWarning($"Invalid file extension: {extension} in 2amanUpload.");
                return (false, $"File type {extension} is not supported. Allowed types: {string.Join(", ", _allowedExtensions)}.");
            }

            if (!SimulateMalwareScan(file))
            {
                _logger.LogError($"Potential malware detected in file: {file.FileName} in 2amanUpload.");
                return (false, "File failed malware scan.");
            }

            return (true, null);
        }

        private bool SimulateMalwareScan(IFormFile file)
        {
            if (file.FileName.ToLower().Contains("malicious"))
            {
                return false;
            }
            return true;
        }
    }
}