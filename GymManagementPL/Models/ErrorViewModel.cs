namespace GymManagementPL.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public int StatusCode { get; set; } = 500;
    }

    public record AvatarModel(string Name, string? PhotoUrl = null, string Size = "md");
}
