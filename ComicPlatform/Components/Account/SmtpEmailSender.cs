using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ComicPlatform.Data;

namespace ComicPlatform.Components.Account;

/// <summary>
/// Bound from the "Email" section of configuration. The defaults are Gmail's.
/// Put User and Password in User Secrets (right-click the project, Manage User
/// Secrets), never in appsettings.json: Password is a Gmail app password, which
/// works like the account password.
/// </summary>
public class EmailSettings
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = "Catkoom";
}

/// <summary>
/// Sends Identity's confirmation and password-reset emails over SMTP. This replaces
/// IdentityNoOpEmailSender. Gmail always sends from the authenticated account, so
/// the From address is the Gmail address in settings. Switching to Resend or Brevo
/// later means changing this one class and the settings.
/// </summary>
internal sealed class SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger)
    : IEmailSender<ApplicationUser>
{
    private readonly EmailSettings settings = options.Value;

    // The links Identity passes in are already HTML-encoded, so they go straight into the body.
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(email, "Confirm your Catkoom email",
            $"Welcome to Catkoom! Please confirm your email by <a href='{confirmationLink}'>clicking here</a>.");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, "Reset your Catkoom password",
            $"You can reset your password by <a href='{resetLink}'>clicking here</a>. " +
            "If you didn't ask for this, you can ignore this email.");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(email, "Reset your Catkoom password",
            $"Reset your password using this code: {resetCode}");

    private async Task SendAsync(string toEmail, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(settings.User) || string.IsNullOrWhiteSpace(settings.Password))
        {
            logger.LogWarning(
                "Email is not configured (Email:User / Email:Password), so \"{Subject}\" to {Email} was not sent.",
                subject, toEmail);
            return;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(settings.User, settings.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(settings.User, settings.Password)
            };

            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            // Logged rather than thrown so a mail problem doesn't turn a finished
            // registration into a 500 page. Check the console output if emails don't arrive.
            logger.LogError(ex, "Failed to send \"{Subject}\" to {Email}.", subject, toEmail);
        }
    }
}
