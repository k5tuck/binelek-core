using System.Net;
using System.Net.Mail;

namespace Binah.Auth.Services;

/// <summary>
/// Email service implementation using SMTP
/// Supports SendGrid, AWS SES, or standard SMTP
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _baseUrl;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _fromEmail = configuration["Email:FromEmail"] ?? "noreply@binelek.io";
        _fromName = configuration["Email:FromName"] ?? "Binelek";
        _baseUrl = configuration["Email:BaseUrl"] ?? "https://app.binelek.io";
    }

    public async Task SendVerificationEmailAsync(string email, string firstName, string verificationToken)
    {
        var verificationUrl = $"{_baseUrl}/verify-email?token={verificationToken}";

        var subject = "Verify your Binelek account";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ color: white; margin: 0; font-size: 24px; }}
        .content {{ background: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; border-top: none; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; }}
        .button:hover {{ background: #5a67d8; }}
        .footer {{ text-align: center; margin-top: 20px; color: #6b7280; font-size: 12px; }}
        .code {{ background: #e5e7eb; padding: 10px 15px; border-radius: 4px; font-family: monospace; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to Binelek</h1>
        </div>
        <div class='content'>
            <p>Hi {firstName},</p>
            <p>Thank you for signing up for Binelek! Please verify your email address to complete your registration.</p>
            <p style='text-align: center;'>
                <a href='{verificationUrl}' class='button'>Verify Email Address</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p class='code'>{verificationUrl}</p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't create a Binelek account, you can safely ignore this email.</p>
            <p>Best regards,<br/>The Binelek Team</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 Binelek. All rights reserved.</p>
            <p>123 Tech Street, San Francisco, CA 94105</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, subject, body, isHtml: true);
        _logger.LogInformation("Verification email sent to {Email}", email);
    }

    public async Task SendPasswordResetEmailAsync(string email, string firstName, string resetToken)
    {
        var resetUrl = $"{_baseUrl}/reset-password?token={resetToken}";

        var subject = "Reset your Binelek password";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ color: white; margin: 0; font-size: 24px; }}
        .content {{ background: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; border-top: none; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #6b7280; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Reset</h1>
        </div>
        <div class='content'>
            <p>Hi {firstName},</p>
            <p>We received a request to reset your Binelek password. Click the button below to create a new password:</p>
            <p style='text-align: center;'>
                <a href='{resetUrl}' class='button'>Reset Password</a>
            </p>
            <p>This link will expire in 1 hour.</p>
            <p>If you didn't request a password reset, you can safely ignore this email. Your password will not be changed.</p>
            <p>Best regards,<br/>The Binelek Team</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 Binelek. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, subject, body, isHtml: true);
        _logger.LogInformation("Password reset email sent to {Email}", email);
    }

    public async Task SendWelcomeEmailAsync(string email, string firstName, string companyName)
    {
        var dashboardUrl = $"{_baseUrl}/dashboard";

        var subject = "Welcome to Binelek - Let's get started!";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ color: white; margin: 0; font-size: 24px; }}
        .content {{ background: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; border-top: none; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; }}
        .step {{ background: white; padding: 15px; margin: 10px 0; border-radius: 6px; border-left: 4px solid #667eea; }}
        .footer {{ text-align: center; margin-top: 20px; color: #6b7280; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to Binelek!</h1>
        </div>
        <div class='content'>
            <p>Hi {firstName},</p>
            <p>Your account for <strong>{companyName}</strong> is now active! Here's how to get started:</p>

            <div class='step'>
                <strong>1. Create your first ontology</strong><br/>
                Define your data model using our visual ontology builder.
            </div>

            <div class='step'>
                <strong>2. Connect your data sources</strong><br/>
                Import data from databases, APIs, or CSV files.
            </div>

            <div class='step'>
                <strong>3. Explore your knowledge graph</strong><br/>
                Visualize relationships and discover insights.
            </div>

            <div class='step'>
                <strong>4. Invite your team</strong><br/>
                Collaborate with team members.
            </div>

            <p style='text-align: center;'>
                <a href='{dashboardUrl}' class='button'>Go to Dashboard</a>
            </p>

            <p>Need help? Check out our <a href='https://docs.binelek.io'>documentation</a> or contact <a href='mailto:support@binelek.io'>support@binelek.io</a>.</p>

            <p>Best regards,<br/>The Binelek Team</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 Binelek. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, subject, body, isHtml: true);
        _logger.LogInformation("Welcome email sent to {Email}", email);
    }

    public async Task SendTeamInvitationEmailAsync(string email, string inviterName, string companyName, string invitationToken)
    {
        var inviteUrl = $"{_baseUrl}/accept-invite?token={invitationToken}";

        var subject = $"{inviterName} invited you to join {companyName} on Binelek";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ color: white; margin: 0; font-size: 24px; }}
        .content {{ background: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; border-top: none; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #6b7280; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>You're Invited!</h1>
        </div>
        <div class='content'>
            <p>Hi there,</p>
            <p><strong>{inviterName}</strong> has invited you to join <strong>{companyName}</strong> on Binelek, the knowledge graph platform.</p>
            <p style='text-align: center;'>
                <a href='{inviteUrl}' class='button'>Accept Invitation</a>
            </p>
            <p>This invitation will expire in 7 days.</p>
            <p>If you weren't expecting this invitation, you can safely ignore this email.</p>
            <p>Best regards,<br/>The Binelek Team</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 Binelek. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, subject, body, isHtml: true);
        _logger.LogInformation("Team invitation email sent to {Email} from {Inviter}", email, inviterName);
    }

    private async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
    {
        var provider = _configuration["Email:Provider"]?.ToLower() ?? "smtp";

        switch (provider)
        {
            case "sendgrid":
                await SendViaSendGridAsync(to, subject, body, isHtml);
                break;
            case "ses":
                await SendViaSesAsync(to, subject, body, isHtml);
                break;
            case "smtp":
            default:
                await SendViaSmtpAsync(to, subject, body, isHtml);
                break;
        }
    }

    private async Task SendViaSmtpAsync(string to, string subject, string body, bool isHtml)
    {
        var smtpHost = _configuration["Email:Smtp:Host"] ?? "localhost";
        var smtpPort = int.Parse(_configuration["Email:Smtp:Port"] ?? "587");
        var smtpUsername = _configuration["Email:Smtp:Username"];
        var smtpPassword = _configuration["Email:Smtp:Password"];
        var enableSsl = bool.Parse(_configuration["Email:Smtp:EnableSsl"] ?? "true");

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = enableSsl,
            Credentials = !string.IsNullOrEmpty(smtpUsername)
                ? new NetworkCredential(smtpUsername, smtpPassword)
                : null
        };

        var message = new MailMessage
        {
            From = new MailAddress(_fromEmail, _fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };
        message.To.Add(to);

        try
        {
            await client.SendMailAsync(message);
            _logger.LogDebug("Email sent via SMTP to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via SMTP to {To}", to);
            throw;
        }
    }

    private async Task SendViaSendGridAsync(string to, string subject, string body, bool isHtml)
    {
        var apiKey = _configuration["Email:SendGrid:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("SendGrid API key is not configured");
        }

        // Using HttpClient directly instead of SendGrid SDK for simplicity
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            personalizations = new[]
            {
                new { to = new[] { new { email = to } } }
            },
            from = new { email = _fromEmail, name = _fromName },
            subject,
            content = new[]
            {
                new { type = isHtml ? "text/html" : "text/plain", value = body }
            }
        };

        var response = await client.PostAsJsonAsync("https://api.sendgrid.com/v3/mail/send", payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("SendGrid API error: {Error}", error);
            throw new Exception($"SendGrid API error: {response.StatusCode}");
        }

        _logger.LogDebug("Email sent via SendGrid to {To}", to);
    }

    private async Task SendViaSesAsync(string to, string subject, string body, bool isHtml)
    {
        // AWS SES implementation would go here
        // For now, fall back to SMTP which can be configured with SES SMTP credentials
        _logger.LogWarning("AWS SES direct API not implemented, falling back to SMTP");
        await SendViaSmtpAsync(to, subject, body, isHtml);
    }
}
