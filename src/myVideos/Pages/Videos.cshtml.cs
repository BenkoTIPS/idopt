using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;
using System.Collections.Generic;
using System.Threading.Tasks;

public class VideosModel : PageModel
{
    private readonly IConfiguration _config;
    private readonly AzureBlobStorageService _storageService;
    public string ContainerName = "movies";

    public VideosModel(IConfiguration config, AzureBlobStorageService storageService)
    {
        _config = config;
        _storageService = storageService;
        ContainerName = config["azContainer"] ?? "movies";
    }

    public IEnumerable<string> VideoNames { get; private set; } = new List<string>();

    public async Task OnGetAsync(string? container = null, bool autoplay = true)
    {
        if (container != null)
            ContainerName = container;

        VideoNames = await _storageService.GetVideoNamesAsync(ContainerName);
    }

    public async Task<IActionResult> OnGetStreamVideo(string videoName, string? container = null)
    {
        if (string.IsNullOrEmpty(videoName))
            return BadRequest("Video name is required");

        var containerToUse = container ?? ContainerName;

        try
        {
            var blobClient = await _storageService.GetBlobAsync(containerToUse, videoName);
            if (!await blobClient.ExistsAsync())
                return NotFound($"Video '{videoName}' not found in container '{containerToUse}'");

            var stream = await blobClient.OpenReadAsync();
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            if (!contentTypeProvider.TryGetContentType(videoName, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            var rc = File(stream, contentType);
            
            // The key to making this a stream with jump to ability
            rc.EnableRangeProcessing = true;

            return rc;
        }
        catch (Exception ex)
        {
            // Log the exception if you have logging configured
            return StatusCode(500, $"Error streaming video: {ex.Message}");
        }
    }
}
