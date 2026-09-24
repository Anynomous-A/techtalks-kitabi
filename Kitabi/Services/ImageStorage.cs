using Microsoft.Extensions.Options;

namespace Kitabi.Services
{
    // Bound from appsettings.json -> "ImageUpload"
    public class ImageUploadOptions
    {
        public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5 MB per file
        public int MaxFilesPerListing { get; set; } = 10;
        public string Folder { get; set; } = "uploads";               // inside wwwroot
    }

    public record ImageValidationResult(bool Ok, string? Error, string? Extension);

    public interface IImageStorage
    {
        int MaxFilesPerListing { get; }
        Task<ImageValidationResult> ValidateAsync(IFormFile file);
        Task<string> SaveAsync(IFormFile file, string extension); // returns "/uploads/<guid>.ext"
        void Delete(string imageUrl);
    }

    public class ImageStorage : IImageStorage
    {
        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif" };

        private static readonly byte[] PngSig = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private readonly ImageUploadOptions _opt;
        private readonly string _root; //path of the uploads folder

        public ImageStorage(IOptions<ImageUploadOptions> options, IWebHostEnvironment env)
        {
            _opt = options.Value;
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            _root = Path.GetFullPath(Path.Combine(webRoot, _opt.Folder));
            Directory.CreateDirectory(_root);
        }

        public int MaxFilesPerListing => _opt.MaxFilesPerListing;

        public async Task<ImageValidationResult> ValidateAsync(IFormFile file)
        {
            //size
            if (file.Length == 0)
                return new(false, "The file is empty.", null);
            if (file.Length > _opt.MaxFileSizeBytes)
                return new(false, $"File too large. Maximum is {_opt.MaxFileSizeBytes / 1024 / 1024} MB.", null);

            //extension allowed
            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
                return new(false, "Only .jpg, .jpeg, .png and .gif files are allowed.", null);

            //check the magic bytes not the client supplied content type
            var header = new byte[12];
            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);

            var detected = DetectExtension(header.AsSpan(0, read));
            if (detected is null)
                return new(false, "The file content is not a valid JPEG, PNG or GIF image.", null);

            return new(true, null, detected);
        }

        public async Task<string> SaveAsync(IFormFile file, string extension)
        {

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(_root, fileName);

            await using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
            await file.CopyToAsync(fs);

            return $"/{_opt.Folder}/{fileName}";
        }

        public void Delete(string imageUrl)
        {
            // keep only the file name, then make sure the final path is still inside the uploads folder
            var fileName = Path.GetFileName(imageUrl);
            if (string.IsNullOrEmpty(fileName)) return;

            var fullPath = Path.GetFullPath(Path.Combine(_root, fileName));
            if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return;

            if (File.Exists(fullPath)) File.Delete(fullPath);
        }

        private static string? DetectExtension(ReadOnlySpan<byte> h)
        {
            if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";
            if (h.Length >= 8 && h[..8].SequenceEqual(PngSig)) return ".png";
            if (h.Length >= 6 && (h[..6].SequenceEqual("GIF87a"u8) || h[..6].SequenceEqual("GIF89a"u8))) return ".gif";
            return null;
        }
    }
}