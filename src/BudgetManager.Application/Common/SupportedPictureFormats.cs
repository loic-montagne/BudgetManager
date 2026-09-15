namespace BudgetManager.Application.Common;

public static class SupportedPictureFormats
{
    public sealed record PictureFormat(string ContentType, IReadOnlySet<string> FileExtensions)
    {
    }

    public const string JpgContentType = "image/jpeg";
    public const string PngContentType = "image/png";
    public const string WebpContentType = "image/webp";

    public const string JpgFileExtension = "jpg";
    public const string JpegFileExtension = "jpeg";
    public const string PngFileExtension = "png";
    public const string WebpFileExtension = "webp";


    public static readonly IReadOnlySet<PictureFormat> All =
        new HashSet<PictureFormat>()
        {
            new PictureFormat(JpgContentType, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { JpgFileExtension, JpegFileExtension }),
            new PictureFormat(PngContentType, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { PngFileExtension }),
            new PictureFormat(WebpContentType, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { WebpFileExtension })
        };
}
