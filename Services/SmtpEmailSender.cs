using System.Net;
using System.Net.Mail;

namespace YourProject.API.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string to, string subject, string body, List<(byte[] Content, string FileName, string ContentType)>? attachments = null)
    {
        var host = _config["Smtp:Host"];
        var port = int.Parse(_config["Smtp:Port"] ?? "587");
        var username = _config["Smtp:Username"];
        var password = _config["Smtp:Password"];
        var from = _config["Smtp:From"];

        using var message = new MailMessage();
        message.From = new MailAddress(from!);
        message.To.Add(to);
        message.Subject = subject;
        message.Body = body;

        if (attachments != null)
        {
            foreach (var att in attachments)
            {
                var ms = new MemoryStream(att.Content);
                message.Attachments.Add(new Attachment(ms, att.FileName, att.ContentType));
            }
        }

        using var smtp = new SmtpClient(host, port);
        smtp.Credentials = new NetworkCredential(username, password);
        smtp.EnableSsl = true;

        await smtp.SendMailAsync(message);
    }
}