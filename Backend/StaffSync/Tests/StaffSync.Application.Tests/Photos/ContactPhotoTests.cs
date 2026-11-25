using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Photos;
using StaffSync.Application.Features.Validators.ContactValidators;

namespace StaffSync.Application.Tests.Photos;

public class ContactPhotoTests
{
    private static readonly byte[] JpegHeader = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
    private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };

    [Fact]
    public void Jpeg_and_png_are_detected_from_header()
    {
        Assert.Equal("image/jpeg", ImageFormats.DetectContentType(JpegHeader));
        Assert.Equal("image/png", ImageFormats.DetectContentType(PngHeader));
    }

    [Fact]
    public void Non_image_is_rejected_even_with_image_extension()
    {
        // ör. "foto.jpg" adıyla yüklenmiş bir PDF
        Assert.Null(ImageFormats.DetectContentType("%PDF-1.7"u8));
        Assert.Null(ImageFormats.DetectContentType(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Photo_url_points_to_api_and_changes_per_upload()
    {
        var first = ContactPhotoUrls.For(7);
        Thread.Sleep(1);
        var second = ContactPhotoUrls.For(7);
        Assert.StartsWith("api/Contacts/Photo/7?v=", first);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Validator_accepts_small_jpeg()
    {
        var result = new UploadContactPhotoCommandValidator()
            .Validate(new UploadContactPhotoCommand { ContactId = 1, Content = JpegHeader });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_rejects_empty_oversized_and_non_image()
    {
        var validator = new UploadContactPhotoCommandValidator();

        Assert.False(validator.Validate(new UploadContactPhotoCommand { Content = Array.Empty<byte>() }).IsValid);
        Assert.False(validator.Validate(new UploadContactPhotoCommand { Content = "GIF89a"u8.ToArray() }).IsValid);

        var oversized = new byte[ImageFormats.MaxPhotoBytes + 1];
        JpegHeader.CopyTo(oversized, 0);
        Assert.False(validator.Validate(new UploadContactPhotoCommand { Content = oversized }).IsValid);
    }
}
