using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;

namespace PSI1.Api.Tests.Controllers;

public class ProfileControllerTests : IClassFixture<ApiFactory>
{
    // only the first bytes matter, the controller recognises images by their signature
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4];
    // a zip archive starts with "PK"
    private static readonly byte[] ZipBytes = [0x50, 0x4B, 0x03, 0x04, 1, 2, 3, 4];

    private readonly ApiFactory _factory;

    public ProfileControllerTests(ApiFactory factory)
    {
        _factory = factory;
    }

    // sends the bytes as a file upload, the same way the browser does
    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        byte[] bytes,
        string fileName = "photo.png",
        string contentType = "image/png")
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", fileName);

        return await client.PostAsync("api/profile/picture", content);
    }

    // reads the user id from the client's JWT, so the test knows which files belong to it
    private static string GetUserId(HttpClient client)
    {
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        return new JwtSecurityTokenHandler().ReadJwtToken(token).Subject;
    }

    // files user has in the uploads folder as the folder may not exist
    private string[] GetUserFiles(HttpClient client)
    {
        if (!Directory.Exists(_factory.UploadsPath))
        {
            return [];
        }

        return Directory.GetFiles(_factory.UploadsPath, $"{GetUserId(client)}.*");
    }

    [Fact]
    public async Task UploadPicture_ValidPng_CanBeLoadedBack()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var upload = await UploadAsync(client, PngBytes);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var response = await client.GetAsync("api/profile/picture");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PngBytes, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task UploadPicture_ValidJpeg_ReturnsJpegContentType()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var upload = await UploadAsync(client, JpegBytes, fileName: "photo.jpg", contentType: "image/jpeg");
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var response = await client.GetAsync("api/profile/picture");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UploadPicture_ZipRenamedToPng_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        // the name and content type say PNG, but the bytes are a zip archive
        var response = await UploadAsync(client, ZipBytes, fileName: "photo.png", contentType: "image/png");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // nothing should be saved: no file on disk and no picture for the user
        Assert.Empty(GetUserFiles(client));
        var picture = await client.GetAsync("api/profile/picture");
        Assert.Equal(HttpStatusCode.NotFound, picture.StatusCode);
    }

    [Fact]
    public async Task UploadPicture_FileOver2Mb_ReturnsBadRequest()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        // a valid PNG header, but one byte over the 2 MB limit
        var bytes = new byte[(2 * 1024 * 1024) + 1];
        PngBytes.CopyTo(bytes, 0);

        var response = await UploadAsync(client, bytes);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // nothing should be saved: no file on disk and no picture for the user
        Assert.Empty(GetUserFiles(client));
        var picture = await client.GetAsync("api/profile/picture");
        Assert.Equal(HttpStatusCode.NotFound, picture.StatusCode);
    }

    [Fact]
    public async Task UploadPicture_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await UploadAsync(client, PngBytes);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPicture_BeforeUpload_ReturnsNotFound()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("api/profile/picture");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadPicture_JpegThenPng_KeepsOnlyPng()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        await UploadAsync(client, JpegBytes, fileName: "photo.jpg", contentType: "image/jpeg");
        await UploadAsync(client, PngBytes);

        var file = Assert.Single(GetUserFiles(client));
        Assert.EndsWith(".png", file);
    }
}