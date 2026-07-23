using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace ItransitionCourseProject.Services;

public sealed record UploadedImage(string Url, string PublicId);

public interface IImageService {
    Task<UploadedImage> UploadAvatarAsync(IFormFile file, CancellationToken token = default);

    Task DeleteAsync(string? publicId, CancellationToken token = default);
}

public sealed class CloudinaryImageService : IImageService {
    private const long MaxAvatarSize = 5 * 1024 * 1024;
    private const string AvatarFolder = "cv-management/avatars";

    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/heic",
        "image/heif"
    ];

    private static readonly string[] AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".heic",
        ".heif"
    ];

    private readonly IConfiguration _configuration;

    public CloudinaryImageService(IConfiguration configuration) {
        _configuration = configuration;
    }

    public async Task<UploadedImage> UploadAvatarAsync(IFormFile file, CancellationToken token = default) {
        if (file is null || file.Length == 0)
        {
            throw new InvalidOperationException("The image file is empty.");
        }

        if (file.Length > MaxAvatarSize)
        {
            throw new InvalidOperationException("The image cannot be larger than 5 MB.");
        }

        if (!IsAllowedImage(file))
        {
            throw new InvalidOperationException("Only JPG, PNG, WEBP and HEIC images are allowed.");
        }

        await using var stream = file.OpenReadStream();
        var cloudinary = CreateClient();

        var uploadParameters = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = AvatarFolder,
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false,
            AllowedFormats = ["jpg", "jpeg", "png", "webp", "heic", "heif"]
        };

        var result = await cloudinary.UploadAsync(uploadParameters, token);

        if (result.Error is not null)
        {
            throw new InvalidOperationException(result.Error.Message);
        }

        if (result.SecureUrl is null)
        {
            throw new InvalidOperationException("Cloudinary did not return an image URL.");
        }

        return new UploadedImage(result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task DeleteAsync(string? publicId, CancellationToken token = default) {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return;
        }

        token.ThrowIfCancellationRequested();
        var result = await CreateClient().DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image,
            Invalidate = true
        });

        if (result.Error is not null)
        {
            throw new InvalidOperationException(result.Error.Message);
        }
    }

    private Cloudinary CreateClient() {
        var cloudName = GetRequiredSetting("Cloudinary:CloudName");
        var apiKey = GetRequiredSetting("Cloudinary:ApiKey");
        var apiSecret = GetRequiredSetting("Cloudinary:ApiSecret");

        var cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        cloudinary.Api.Secure = true;
        return cloudinary;
    }

    private static bool IsAllowedImage(IFormFile file) {
        if (AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        var extension = Path.GetExtension(file.FileName);

        return AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private string GetRequiredSetting(string name) {
        var value = _configuration[name];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value '{name}' was not found.");
        }

        return value;
    }
}
