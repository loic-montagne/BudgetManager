using BudgetManager.Application.Common;
using BudgetManager.Application.Common.Errors;
using BudgetManager.Application.Features.User.Common;
using BudgetManager.Infrastructure.Identity;
using SkiaSharp;
using Xunit;

namespace BudgetManager.Infrastructure.Tests.Identity;

public sealed class ProfilePictureProcessorTests
{
    private readonly ProfilePictureProcessor _processor =
        new();

    [Fact]
    public void Process_WhenPngIsValid_Produces512SquareWebp()
    {
        // Arrange

        var source =
            CreateImage(
                800,
                600,
                SKEncodedImageFormat.Png);

        var picture =
            new UserProfilePicture(
                source,
                SupportedPictureFormats.PngContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            _processor.Process(
                picture,
                cancellationToken);

        // Assert

        Assert.True(
            result.IsValid);

        Assert.Empty(
            result.Errors);

        Assert.Equal(
            SupportedPictureFormats.WebpContentType,
            result.Value.ContentType);

        using var data =
            SKData.CreateCopy(
                result.Value.Content);

        using var codec =
            SKCodec.Create(
                data);

        Assert.NotNull(
            codec);

        Assert.Equal(
            SKEncodedImageFormat.Webp,
            codec.EncodedFormat);

        Assert.Equal(
            512,
            codec.Info.Width);

        Assert.Equal(
            512,
            codec.Info.Height);
    }

    [Fact]
    public void Process_WhenJpegIsValid_ProducesWebp()
    {
        // Arrange

        var source =
            CreateImage(
                640,
                640,
                SKEncodedImageFormat.Jpeg);

        var picture =
            new UserProfilePicture(
                source,
                SupportedPictureFormats.JpgContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            _processor.Process(
                picture,
                cancellationToken);

        // Assert

        Assert.True(
            result.IsValid);

        Assert.Equal(
            SupportedPictureFormats.WebpContentType,
            result.Value.ContentType);
    }

    [Fact]
    public void Process_WhenWebpIsValid_Produces512SquareWebp()
    {
        // Arrange

        var source =
            CreateImage(
                700,
                900,
                SKEncodedImageFormat.Webp);

        var picture =
            new UserProfilePicture(
                source,
                SupportedPictureFormats.WebpContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            _processor.Process(
                picture,
                cancellationToken);

        // Assert

        Assert.True(
            result.IsValid);

        using var data =
            SKData.CreateCopy(
                result.Value.Content);

        using var codec =
            SKCodec.Create(
                data);

        Assert.NotNull(
            codec);

        Assert.Equal(
            SKEncodedImageFormat.Webp,
            codec.EncodedFormat);

        Assert.Equal(
            512,
            codec.Info.Width);

        Assert.Equal(
            512,
            codec.Info.Height);
    }

    [Fact]
    public void Process_WhenBytesAreNotAnImage_ReturnsInvalidError()
    {
        // Arrange

        var picture =
            new UserProfilePicture(
                [1, 2, 3, 4, 5],
                SupportedPictureFormats.PngContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            _processor.Process(
                picture,
                cancellationToken);

        // Assert

        Assert.False(
            result.IsValid);

        Assert.Contains(
            result.Errors,
            x => x.Code ==
                ErrorCodes.UserProfilePictureInvalid);
    }

    [Fact]
    public void Process_WhenEncodedFormatIsUnsupported_ReturnsExpectedError()
    {
        // Arrange

        var gif =
            Convert.FromBase64String(
                "R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==");

        var picture =
            new UserProfilePicture(
                gif,
                "image/gif");

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            _processor.Process(
                picture,
                cancellationToken);

        // Assert

        Assert.False(
            result.IsValid);

        Assert.Contains(
            result.Errors,
            x => x.Code ==
                ErrorCodes.UserProfilePictureFormatUnsupported);
    }

    [Fact]
    public void Process_WhenDeclaredContentTypeDoesNotMatchImage_ReturnsInvalidError()
    {
        // Arrange

        var source =
            CreateImage(
                512,
                512,
                SKEncodedImageFormat.Png);

        var picture =
            new UserProfilePicture(
                source,
                SupportedPictureFormats.JpgContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            _processor.Process(
                picture,
                cancellationToken);

        // Assert

        Assert.False(
            result.IsValid);

        Assert.Contains(
            result.Errors,
            x => x.Code ==
                ErrorCodes.UserProfilePictureInvalid);
    }

    [Fact]
    public void Process_WhenDimensionsAreTooSmall_ReturnsExpectedError()
    {
        // Arrange

        var source =
            CreateImage(
                63,
                64,
                SKEncodedImageFormat.Png);

        var picture =
            new UserProfilePicture(
                source,
                SupportedPictureFormats.PngContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            _processor.Process(
                picture,
                cancellationToken);

        // Assert

        Assert.False(
            result.IsValid);

        Assert.Contains(
            result.Errors,
            x => x.Code ==
                ErrorCodes.UserProfilePictureDimensionsTooSmall);
    }

    [Fact]
    public void Process_WhenDimensionsAreTooLarge_ReturnsExpectedError()
    {
        // Arrange

        var source =
            CreateImage(
                4097,
                64,
                SKEncodedImageFormat.Png);

        var picture =
            new UserProfilePicture(
                source,
                SupportedPictureFormats.PngContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            _processor.Process(
                picture,
                cancellationToken);

        // Assert

        Assert.False(
            result.IsValid);

        Assert.Contains(
            result.Errors,
            x => x.Code ==
                ErrorCodes.UserProfilePictureDimensionsTooLarge);
    }

    [Theory]
    [InlineData(64, 64)]
    [InlineData(4096, 64)]
    [InlineData(64, 4096)]
    public void Process_WhenDimensionsAreOnAcceptedBoundaries_IsValid(
        int width,
        int height)
    {
        // Arrange

        var source =
            CreateImage(
                width,
                height,
                SKEncodedImageFormat.Png);

        var picture =
            new UserProfilePicture(
                source,
                SupportedPictureFormats.PngContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            _processor.Process(
                picture,
                cancellationToken);

        // Assert

        Assert.True(
            result.IsValid);
    }

    [Fact]
    public void Process_WhenJpegContainsOrientationMetadata_ProcessesImageSuccessfully()
    {
        // Arrange

        var source =
            AddExifOrientation(
                CreateImage(
                    100,
                    200,
                    SKEncodedImageFormat.Jpeg),
                orientation: 6);

        using var sourceData =
            SKData.CreateCopy(
                source);

        using var sourceCodec =
            SKCodec.Create(
                sourceData);

        Assert.NotNull(
            sourceCodec);

        Assert.Equal(
            SKEncodedOrigin.RightTop,
            sourceCodec.EncodedOrigin);

        var picture =
            new UserProfilePicture(
                source,
                SupportedPictureFormats.JpgContentType);

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var result =
            _processor.Process(
                picture,
                cancellationToken);

        // Assert

        Assert.True(
            result.IsValid);

        using var outputData =
            SKData.CreateCopy(
                result.Value.Content);

        using var outputCodec =
            SKCodec.Create(
                outputData);

        Assert.NotNull(
            outputCodec);

        Assert.Equal(
            512,
            outputCodec.Info.Width);

        Assert.Equal(
            512,
            outputCodec.Info.Height);
    }

    [Fact]
    public void Process_WhenCancellationIsRequested_ThrowsOperationCanceledException()
    {
        // Arrange

        var source =
            CreateImage(
                512,
                512,
                SKEncodedImageFormat.Png);

        var picture =
            new UserProfilePicture(
                source,
                SupportedPictureFormats.PngContentType);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        // Act

        var action = () =>
            _processor.Process(
                picture,
                cancellationTokenSource.Token);

        // Assert

        Assert.Throws<OperationCanceledException>(
            action);
    }

    [Fact]
    public void Process_WhenPictureIsNull_ThrowsArgumentNullException()
    {
        // Arrange

        var cancellationToken =
            TestContext.Current.CancellationToken;

        // Act

        var action = () =>
            _processor.Process(
                null!,
                cancellationToken);

        // Assert

        Assert.Throws<ArgumentNullException>(
            action);
    }

    private static byte[] CreateImage(
        int width,
        int height,
        SKEncodedImageFormat format)
    {
        using var bitmap =
            new SKBitmap(
                width,
                height,
                SKColorType.Rgba8888,
                SKAlphaType.Premul);

        bitmap.Erase(
            SKColors.CornflowerBlue);

        using var image =
            SKImage.FromBitmap(
                bitmap);

        using var encoded =
            image.Encode(
                format,
                90);

        Assert.NotNull(
            encoded);

        return encoded.ToArray();
    }

    private static byte[] AddExifOrientation(
        byte[] jpeg,
        ushort orientation)
    {
        Assert.True(
            jpeg.Length >= 2
            && jpeg[0] == 0xFF
            && jpeg[1] == 0xD8);

        byte[] exifPayload =
        [
            0x45, 0x78, 0x69, 0x66, 0x00, 0x00,
            0x49, 0x49, 0x2A, 0x00,
            0x08, 0x00, 0x00, 0x00,
            0x01, 0x00,
            0x12, 0x01,
            0x03, 0x00,
            0x01, 0x00, 0x00, 0x00,
            (byte)(orientation & 0xFF),
            (byte)(orientation >> 8),
            0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        ];

        var segmentLength =
            exifPayload.Length + 2;

        var result =
            new byte[
                jpeg.Length
                + exifPayload.Length
                + 4];

        result[0] =
            0xFF;

        result[1] =
            0xD8;

        result[2] =
            0xFF;

        result[3] =
            0xE1;

        result[4] =
            (byte)(segmentLength >> 8);

        result[5] =
            (byte)(segmentLength & 0xFF);

        exifPayload.CopyTo(
            result,
            6);

        Array.Copy(
            jpeg,
            2,
            result,
            6 + exifPayload.Length,
            jpeg.Length - 2);

        return result;
    }
}
