using Application.Abstractions.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using Shouldly;
using Web.Api.Endpoints.Posts;

namespace Application.UnitTests.Endpoints.Posts;

public class UploadAttachmentHandlerTests
{
    private readonly IAttachmentStorage _storage = Substitute.For<IAttachmentStorage>();

    [Fact]
    public async Task Handle_ValidImage_Returns200WithUrl()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var file = CreateFormFile("photo.jpg", "image/jpeg", 1024);
        _storage.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), "image/jpeg")
            .Returns("https://garage/nexus-attachments/posts/id/file.jpg");

        // Act
        IResult result = await UploadAttachment.Handle(postId, file, _storage);

        // Assert
        Ok<object> ok = result.ShouldBeOfType<Ok<object>>();
        ok.Value.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_ForbiddenContentType_Returns400()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var file = CreateFormFile("script.exe", "application/x-msdownload", 512);

        // Act
        IResult result = await UploadAttachment.Handle(postId, file, _storage);

        // Assert
        result.ShouldBeOfType<BadRequest<object>>();
        await _storage.DidNotReceive().UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_FileTooLarge_Returns400()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var file = CreateFormFile("big.jpg", "image/jpeg", 21 * 1024 * 1024);

        // Act
        IResult result = await UploadAttachment.Handle(postId, file, _storage);

        // Assert
        result.ShouldBeOfType<BadRequest<object>>();
        await _storage.DidNotReceive().UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_ValidPdf_Returns200WithUrl()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var file = CreateFormFile("doc.pdf", "application/pdf", 2048);
        _storage.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), "application/pdf")
            .Returns("https://garage/nexus-attachments/posts/id/doc.pdf");

        // Act
        IResult result = await UploadAttachment.Handle(postId, file, _storage);

        // Assert
        result.ShouldBeOfType<Ok<object>>();
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, long length)
    {
        IFormFile file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.ContentType.Returns(contentType);
        file.Length.Returns(length);
        file.OpenReadStream().Returns(new MemoryStream(new byte[Math.Min(length, 100)]));
        return file;
    }
}
