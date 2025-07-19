using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

[Authorize]
public class UploadModel : PageModel
{
    private readonly IConfiguration _config;
    private readonly AzureBlobStorageService _storageService;
    public string ContainerName = "movies";

    public UploadModel(IConfiguration config, AzureBlobStorageService storageService)
    {
        _config = config;
        _storageService = storageService;
        ContainerName = config["azContainer"] ?? "movies";
    }

    [BindProperty]
    [Required(ErrorMessage = "Please select a video file.")]
    [Display(Name = "Video File")]
    public IFormFile? VideoFile { get; set; }

    [TempData]
    public string? Message { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        // Page load - no action needed
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (VideoFile == null || VideoFile.Length == 0)
        {
            ErrorMessage = "Please select a video file to upload.";
            return Page();
        }

        // Validate file type (basic check)
        var allowedExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };
        var fileExtension = Path.GetExtension(VideoFile.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(fileExtension))
        {
            ErrorMessage = $"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}";
            return Page();
        }

        // Check file size (limit to 500MB)
        const long maxFileSize = 500 * 1024 * 1024; // 500MB
        if (VideoFile.Length > maxFileSize)
        {
            ErrorMessage = "File size cannot exceed 500MB.";
            return Page();
        }

        try
        {
            // Generate unique filename to avoid conflicts
            var fileName = $"{Guid.NewGuid()}_{VideoFile.FileName}";

            using var stream = VideoFile.OpenReadStream();
            var uploadedUrl = await _storageService.UploadFileAsync(stream, fileName, ContainerName);

            Message = $"Successfully uploaded '{VideoFile.FileName}' to container '{ContainerName}'.";

            // Clear the form
            VideoFile = null;
            ModelState.Clear();

            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error uploading file: {ex.Message}";
            return Page();
        }
    }
}
