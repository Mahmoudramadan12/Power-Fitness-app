using GymManagementBLL.Services.AttachmentService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace GymManagement.Tests
{
    /// <summary>These run against a real temp directory: they test the actual file handling.</summary>
    public class AttachmentServiceTests : IDisposable
    {
        private const string Folder = "Pics";
        private readonly string _root = Path.Combine(Path.GetTempPath(), "gym-tests-" + Guid.NewGuid().ToString("N"));
        private readonly AttachmentService _sut;

        private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];
        private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4];

        public AttachmentServiceTests()
        {
            Directory.CreateDirectory(_root);
            _sut = new AttachmentService(new TestEnv(_root), NullLogger<AttachmentService>.Instance);
        }

        public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

        private string Dir => Path.Combine(_root, Folder);

        [Fact]
        public async Task A_real_jpeg_and_a_real_png_are_stored_under_generated_names()
        {
            var jpg = await _sut.UploadAsync(new MemoryStream(Jpeg), "holiday.jpg", Folder);
            var png = await _sut.UploadAsync(new MemoryStream(Png), "logo.PNG", Folder);     // upper-case extension

            Assert.NotNull(jpg); Assert.NotNull(png);
            Assert.NotEqual("holiday.jpg", jpg);
            Assert.True(File.Exists(Path.Combine(Dir, jpg!)));
            Assert.Equal(Jpeg.Length, new FileInfo(Path.Combine(Dir, jpg!)).Length);
            Assert.EndsWith(".png", png!);
        }

        [Fact]
        public async Task Content_must_match_the_extension()
        {
            var exeAsJpg = await _sut.UploadAsync(new MemoryStream([0x4D, 0x5A, 0x90, 0x00, 1, 2, 3, 4, 5, 6, 7, 8]), "virus.jpg", Folder);
            var jpegAsPng = await _sut.UploadAsync(new MemoryStream(Jpeg), "photo.png", Folder);
            var pngAsJpg = await _sut.UploadAsync(new MemoryStream(Png), "photo.jpg", Folder);

            Assert.Null(exeAsJpg);
            Assert.Null(jpegAsPng);
            Assert.Null(pngAsJpg);
            Assert.True(!Directory.Exists(Dir) || Directory.GetFiles(Dir).Length == 0);
        }

        [Fact]
        public async Task Disallowed_extensions_and_empty_files_are_rejected()
        {
            Assert.Null(await _sut.UploadAsync(new MemoryStream(Jpeg), "page.html", Folder));
            Assert.Null(await _sut.UploadAsync(new MemoryStream(Jpeg), "image.svg", Folder));
            Assert.Null(await _sut.UploadAsync(new MemoryStream([]), "empty.jpg", Folder));
        }

        [Fact]
        public async Task Files_over_five_megabytes_are_rejected_and_not_left_behind()
        {
            var big = new byte[5 * 1024 * 1024 + 100];
            Array.Copy(Jpeg, big, Jpeg.Length);

            var result = await _sut.UploadAsync(new MemoryStream(big), "big.jpg", Folder);

            Assert.Null(result);
            Assert.Empty(Directory.GetFiles(Dir));
        }

        [Fact]
        public async Task Delete_removes_the_file_from_the_same_folder_it_was_uploaded_to()
        {
            // Regression: delete used a different base folder than upload, so photos were never removed.
            var name = (await _sut.UploadAsync(new MemoryStream(Png), "a.png", Folder))!;

            Assert.True(_sut.Delete(name, Folder));
            Assert.False(File.Exists(Path.Combine(Dir, name)));
            Assert.False(_sut.Delete(name, Folder));
        }

        [Fact]
        public async Task Path_traversal_cannot_escape_the_folder()
        {
            var secret = Path.Combine(_root, "secret.txt");
            File.WriteAllText(secret, "top secret");
            await _sut.UploadAsync(new MemoryStream(Png), "a.png", Folder);

            Assert.Null(_sut.GetFile("../secret.txt", Folder));
            Assert.False(_sut.Delete("../secret.txt", Folder));
            Assert.True(File.Exists(secret));
        }

        [Fact]
        public async Task GetFile_returns_the_content_type()
        {
            var name = (await _sut.UploadAsync(new MemoryStream(Png), "a.png", Folder))!;

            var file = _sut.GetFile(name, Folder);
            Assert.NotNull(file);
            using (file!.Value.Stream) Assert.Equal("image/png", file.Value.ContentType);
            Assert.Null(_sut.GetFile("missing.png", Folder));
        }

        private sealed class TestEnv(string root) : IWebHostEnvironment
        {
            public string ApplicationName { get; set; } = "Tests";
            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
            public string WebRootPath { get; set; } = root;
            public string EnvironmentName { get; set; } = "Test";
            public string ContentRootPath { get; set; } = root;
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
