using BudgetManager.Application.Abstractions.Identity;
using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Features.User.Common;
using SkiaSharp;

namespace BudgetManager.Infrastructure.Identity;

internal sealed class ProfilePictureProcessor : IProfilePictureProcessor
{
    private const int TargetSize = 512;
    private const int MaxSize = 4096;
    private const int MinSize = 64;
    private const int WebpQuality = 85;

    public Result<UserProfilePicture> Process(UserProfilePicture picture, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(picture);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var data = SKData.CreateCopy(picture.Content);
            using var codec = SKCodec.Create(data);

            if (codec is null)
                return Invalid(picture, ErrorCodes.UserProfilePictureInvalid, "Invalid profile picture.");

            if (!TryGetContentType(codec.EncodedFormat, out var actualContentType))
                return Invalid(picture, ErrorCodes.UserProfilePictureFormatUnsupported, "Profile picture format is not supported.");

            if (!string.Equals(actualContentType, picture.ContentType, StringComparison.OrdinalIgnoreCase))
                return Invalid(picture, ErrorCodes.UserProfilePictureInvalid, "Profile picture content does not match its declared content type.");

            var info = codec.Info;
            if (info.Width > MaxSize || info.Height > MaxSize)
                return Invalid(picture, ErrorCodes.UserProfilePictureDimensionsTooLarge, "Profile picture dimensions are too large.");
            if (info.Width < MinSize || info.Height < MinSize)
                return Invalid(picture, ErrorCodes.UserProfilePictureDimensionsTooSmall, "Profile picture dimensions are too small.");

            cancellationToken.ThrowIfCancellationRequested();

            using var decoded = SKBitmap.Decode(codec);

            if (decoded is null)
                return Invalid(picture, ErrorCodes.UserProfilePictureInvalid, "Invalid profile picture.");

            using var oriented = ApplyOrientation(decoded, codec.EncodedOrigin);

            cancellationToken.ThrowIfCancellationRequested();

            using var surface =
                SKSurface.Create(
                    new SKImageInfo(
                        TargetSize,
                        TargetSize,
                        SKColorType.Rgba8888,
                        SKAlphaType.Premul));

            if (surface is null)
                return Invalid(picture, ErrorCodes.UserProfilePictureInvalid, "Unable to process profile picture.");

            var cropSize = Math.Min(oriented.Width, oriented.Height);
            var cropLeft = (oriented.Width - cropSize) / 2f;
            var cropTop = (oriented.Height - cropSize) / 2f;
            var sourceRect = new SKRect(cropLeft, cropTop, cropLeft + cropSize, cropTop + cropSize);
            var destinationRect = new SKRect(0, 0, TargetSize, TargetSize);

            using var orientedImage = SKImage.FromBitmap(oriented);

            var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
            
            surface.Canvas.Clear(SKColors.Transparent);
            surface.Canvas.DrawImage(orientedImage, sourceRect, destinationRect, sampling);
            surface.Canvas.Flush();

            cancellationToken.ThrowIfCancellationRequested();

            using var processedImage = surface.Snapshot();

            using var encoded = processedImage.Encode(SKEncodedImageFormat.Webp, WebpQuality);
            if (encoded is null)
                return Invalid(picture, ErrorCodes.UserProfilePictureInvalid, "Unable to encode profile picture.");

            return new Result<UserProfilePicture>(new UserProfilePicture(encoded.ToArray(), SupportedPictureFormats.WebpContentType), []);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (OutOfMemoryException)
        {
            throw;
        }
        catch
        {
            return Invalid(picture, ErrorCodes.UserProfilePictureInvalid, "Invalid profile picture.");
        }
    }

    private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        var width = source.Width;
        var height = source.Height;
        Action<SKCanvas> transform = _ => { };

        switch (origin)
        {
            case SKEncodedOrigin.TopRight:
                transform = canvas => canvas.Scale(-1, 1, width / 2f, height / 2f);
                break;

            case SKEncodedOrigin.BottomRight:
                transform = canvas => canvas.RotateDegrees(180, width / 2f, height / 2f);
                break;

            case SKEncodedOrigin.BottomLeft:
                transform = canvas => canvas.Scale(1, -1, width / 2f, height / 2f);
                break;

            case SKEncodedOrigin.LeftTop:
                width = source.Height;
                height = source.Width;
                transform = canvas =>
                {
                    canvas.RotateDegrees(90, width / 2f, height / 2f);
                    canvas.Scale(height / (float)width, -width / (float)height, width / 2f, height / 2f);
                };
                break;

            case SKEncodedOrigin.RightTop:
                width = source.Height;
                height = source.Width;
                transform = canvas =>
                {
                    canvas.RotateDegrees(90, width / 2f, height / 2f);
                    canvas.Scale(height / (float)width, width / (float)height, width / 2f, height / 2f);
                };
                break;

            case SKEncodedOrigin.RightBottom:
                width = source.Height;
                height = source.Width;
                transform = canvas =>
                {
                    canvas.RotateDegrees(90, width / 2f, height / 2f);
                    canvas.Scale(-height / (float)width, width / (float)height, width / 2f, height / 2f);
                };
                break;

            case SKEncodedOrigin.LeftBottom:
                width = source.Height;
                height = source.Width;
                transform = canvas =>
                {
                    canvas.RotateDegrees(90, width / 2f, height / 2f);
                    canvas.Scale(-height / (float)width, -width / (float)height, width / 2f, height / 2f);
                };
                break;
        }

        var oriented = new SKBitmap(
            width,
            height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);

        using var canvas = new SKCanvas(oriented);

        canvas.Clear(SKColors.Transparent);

        transform(canvas);

        canvas.DrawBitmap(
            source,
            new SKRect(0, 0, width, height),
            new SKSamplingOptions(SKFilterMode.Linear));

        canvas.Flush();

        return oriented;
    }

    private static bool TryGetContentType(SKEncodedImageFormat format, out string contentType)
    {
        contentType =
            format switch
            {
                SKEncodedImageFormat.Jpeg =>
                    SupportedPictureFormats.JpgContentType,
                SKEncodedImageFormat.Png =>
                    SupportedPictureFormats.PngContentType,
                SKEncodedImageFormat.Webp =>
                    SupportedPictureFormats.WebpContentType,
                _ =>
                    string.Empty
            };
        return contentType.Length > 0;
    }

    private static Result<UserProfilePicture> Invalid(UserProfilePicture picture, string errorCode, string message)
    {
        return new Result<UserProfilePicture>(picture, [(errorCode, message)]);
    }
}
