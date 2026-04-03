namespace YourProject.API.DTOs.Tracking;

public class UploadTrackingFileResponse
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long FileSize { get; set; }
    public string ContentType { get; set; } = "";
}