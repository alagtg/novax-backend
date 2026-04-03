using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using YourProject.API.Helpers;

public class EmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(
        List<string> recipients,
        string subject,
        string body,
        List<IFormFile>? attachments)
    {
        using var smtp = new SmtpClient(_settings.SmtpServer)
        {
            Port = _settings.Port,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            EnableSsl = true
        };

        var mail = new MailMessage
        {
            From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        foreach (var r in recipients)
            mail.Bcc.Add(r); // Bcc pour confidentialité

        if (attachments != null)
        {
            foreach (var file in attachments)
            {
                var stream = file.OpenReadStream();
                mail.Attachments.Add(new Attachment(stream, file.FileName));
            }
        }

        await smtp.SendMailAsync(mail);
    }
}