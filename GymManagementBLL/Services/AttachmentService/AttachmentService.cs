using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace GymManagementBLL.Services.AttachmentService
{
    /// <summary>
    /// Stores uploads under <c>{ContentRoot}/{folder}</c> - deliberately outside wwwroot, so files are only
    /// reachable through an authorised controller action - using a generated file name.
    /// </summary>
    public class AttachmentService(IWebHostEnvironment env, ILogger<AttachmentService> logger) : IAttachmentService
    {
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
        private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        public async Task<string?> UploadAsync(Stream fileStream, string fileName, string folderName, CancellationToken ct = default)
        {
            if (fileStream is null || !fileStream.CanRead) return null;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (extension is not (".jpg" or ".jpeg" or ".png"))
            {
                logger.LogWarning("Rejected upload: extension {Ext} not allowed.", extension);
                return null;
            }

            // Verify the content really is the image type the extension claims (not an .exe renamed to .jpg).
            var header = new byte[PngSignature.Length];
            var read = await fileStream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
            var isPng = read >= PngSignature.Length && header.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature);
            var isJpeg = read >= JpegSignature.Length && header.AsSpan(0, JpegSignature.Length).SequenceEqual(JpegSignature);

            if ((extension == ".png" && !isPng) || (extension != ".png" && !isJpeg))
            {
                logger.LogWarning("Rejected upload: content does not match the {Ext} extension.", extension);
                return null;
            }

            var folder = ResolveFolder(folderName);
            Directory.CreateDirectory(folder);

            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(folder, storedFileName);

            try
            {
                await using (var fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await fs.WriteAsync(header.AsMemory(0, read), ct);
                    long total = read;

                    var buffer = new byte[81920];
                    int n;
                    while ((n = await fileStream.ReadAsync(buffer, ct)) > 0)
                    {
                        total += n;
                        if (total > MaxFileSize)
                        {
                            fs.Close();
                            File.Delete(filePath);
                            logger.LogWarning("Rejected upload: file larger than {Max} bytes.", MaxFileSize);
                            return null;
                        }
                        await fs.WriteAsync(buffer.AsMemory(0, n), ct);
                    }

                    if (total == 0) { fs.Close(); File.Delete(filePath); return null; }
                }

                return storedFileName;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to store uploaded file {FileName}.", fileName);
                TryDelete(filePath);
                return null;
            }
        }

        public bool Delete(string fileName, string folderName)
        {
            var path = ResolveFile(fileName, folderName);
            if (path is null || !File.Exists(path)) return false;

            try { File.Delete(path); return true; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete attachment {File}.", fileName);
                return false;
            }
        }

        public (Stream Stream, string ContentType)? GetFile(string fileName, string folderName)
        {
            var path = ResolveFile(fileName, folderName);
            if (path is null || !File.Exists(path)) return null;

            var contentType = Path.GetExtension(path).ToLowerInvariant() == ".png" ? "image/png" : "image/jpeg";
            return (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), contentType);
        }

        private string ResolveFolder(string folderName) => Path.Combine(env.ContentRootPath, folderName);

        /// <summary>Resolves a stored file name safely: only the bare file name is used, so "../" tricks are inert.</summary>
        private string? ResolveFile(string fileName, string folderName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(folderName)) return null;
            return Path.Combine(ResolveFolder(folderName), Path.GetFileName(fileName));
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
        }
    }
}
