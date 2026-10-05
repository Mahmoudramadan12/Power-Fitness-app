namespace GymManagementBLL.Services.AttachmentService
{
    public interface IAttachmentService
    {
        /// <summary>
        /// Validates (size, extension AND file signature) and stores an image under a generated name.
        /// Returns the stored file name, or <c>null</c> when the file was rejected.
        /// </summary>
        Task<string?> UploadAsync(Stream fileStream, string fileName, string folderName, CancellationToken ct = default);

        bool Delete(string fileName, string folderName);

        (Stream Stream, string ContentType)? GetFile(string fileName, string folderName);
    }
}
