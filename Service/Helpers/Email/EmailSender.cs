using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

namespace Service.Helpers.Email;

public sealed record EmailMessage(string To, string Subject, string Text, string Html);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public sealed class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
}

public sealed class EmailOptions
{
    public string From { get; set; } = "Portico <no-reply@portico.az>";

    // The Web page a confirmation link opens. It comes from configuration, never from the request, so nobody can
    // make Portico send its email with a link to somewhere else.
    public string ConfirmUrl { get; set; } = "";

    // Where emails are written as .eml files when no SMTP host is set. Made absolute at startup.
    public string PickupDirectory { get; set; } = "App_Data/mail";

    public SmtpSettings Smtp { get; set; } = new();

    public bool UsesSmtp => !string.IsNullOrWhiteSpace(Smtp.Host);
}

// Sends through SMTP when a host is configured; otherwise writes each email to the pickup folder,
// so development needs no mail account. Open the .eml file to read it.
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(EmailOptions options, ILogger<SmtpEmailSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        using var mail = new MailMessage
        {
            From = new MailAddress(_options.From),
            Subject = message.Subject,
            Body = message.Text,
            IsBodyHtml = false
        };
        mail.To.Add(message.To);
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.Html, null, "text/html"));

        using var client = new SmtpClient();
        if (_options.UsesSmtp)
        {
            client.Host = _options.Smtp.Host;
            client.Port = _options.Smtp.Port;
            client.EnableSsl = _options.Smtp.EnableSsl;
            if (!string.IsNullOrWhiteSpace(_options.Smtp.User))
                client.Credentials = new NetworkCredential(_options.Smtp.User, _options.Smtp.Password);
        }
        else
        {
            Directory.CreateDirectory(_options.PickupDirectory);
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = _options.PickupDirectory;
        }

        await client.SendMailAsync(mail, cancellationToken);
        _logger.LogInformation(_options.UsesSmtp ? "Sent \"{Subject}\" to {To}." : "Wrote \"{Subject}\" for {To} to the mail folder.", message.Subject, message.To);
    }
}
