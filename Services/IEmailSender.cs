namespace YourProject.API.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, List<(byte[] Content, string FileName, string ContentType)>? attachments = null);
}