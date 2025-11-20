namespace Binah.Auth.Services;

/// <summary>
/// Interface for email sending functionality
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a verification email to a new user
    /// </summary>
    Task SendVerificationEmailAsync(string email, string firstName, string verificationToken);

    /// <summary>
    /// Send a password reset email
    /// </summary>
    Task SendPasswordResetEmailAsync(string email, string firstName, string resetToken);

    /// <summary>
    /// Send a welcome email after verification
    /// </summary>
    Task SendWelcomeEmailAsync(string email, string firstName, string companyName);

    /// <summary>
    /// Send a team invitation email
    /// </summary>
    Task SendTeamInvitationEmailAsync(string email, string inviterName, string companyName, string invitationToken);
}
