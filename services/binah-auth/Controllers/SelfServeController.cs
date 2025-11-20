using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using System.Text;
using Binah.Auth.Services;

namespace Binah.Auth.Controllers;

/// <summary>
/// Controller for self-serve SMB signup without sales calls
/// Phase 1 SMB Implementation
/// </summary>
[ApiController]
[Route("api/self-serve")]
public class SelfServeController : ControllerBase
{
    private readonly ILogger<SelfServeController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public SelfServeController(
        ILogger<SelfServeController> logger,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _logger = logger;
        _configuration = configuration;
        _emailService = emailService;
    }

    /// <summary>
    /// Get available pricing tiers for SMB
    /// </summary>
    [HttpGet("pricing-tiers")]
    [AllowAnonymous]
    public ActionResult<List<PricingTier>> GetPricingTiers()
    {
        var tiers = new List<PricingTier>
        {
            new()
            {
                Id = "solo",
                Name = "Solo",
                Price = 49,
                Interval = "month",
                Description = "Perfect for freelancers and solo entrepreneurs",
                Features = new List<string>
                {
                    "1 user",
                    "10,000 entities",
                    "5 data integrations",
                    "Basic search",
                    "Email support",
                    "5 GB storage"
                },
                Limits = new TierLimits
                {
                    Users = 1,
                    Entities = 10000,
                    Integrations = 5,
                    ApiCallsPerMonth = 10000,
                    StorageGb = 5
                },
                StripePriceId = _configuration["Stripe:SoloPriceId"] ?? "price_solo_monthly"
            },
            new()
            {
                Id = "team",
                Name = "Team",
                Price = 149,
                Interval = "month",
                Description = "Great for small teams and growing businesses",
                Features = new List<string>
                {
                    "5 users",
                    "50,000 entities",
                    "15 data integrations",
                    "Semantic search",
                    "Priority support",
                    "25 GB storage",
                    "Basic AI insights"
                },
                Limits = new TierLimits
                {
                    Users = 5,
                    Entities = 50000,
                    Integrations = 15,
                    ApiCallsPerMonth = 50000,
                    StorageGb = 25
                },
                StripePriceId = _configuration["Stripe:TeamPriceId"] ?? "price_team_monthly",
                Popular = true
            },
            new()
            {
                Id = "business",
                Name = "Business",
                Price = 499,
                Interval = "month",
                Description = "For established businesses with complex data needs",
                Features = new List<string>
                {
                    "25 users",
                    "250,000 entities",
                    "Unlimited integrations",
                    "Advanced semantic search",
                    "Dedicated support",
                    "100 GB storage",
                    "Full AI suite",
                    "Custom ontology templates",
                    "API access"
                },
                Limits = new TierLimits
                {
                    Users = 25,
                    Entities = 250000,
                    Integrations = -1, // Unlimited
                    ApiCallsPerMonth = 500000,
                    StorageGb = 100
                },
                StripePriceId = _configuration["Stripe:BusinessPriceId"] ?? "price_business_monthly"
            }
        };

        return Ok(tiers);
    }

    /// <summary>
    /// Sign up for a new account (self-serve, no sales call)
    /// </summary>
    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<ActionResult<SignupResponse>> Signup([FromBody] SignupRequest request)
    {
        _logger.LogInformation("Self-serve signup attempt for {Email}", request.Email);

        // Validate required fields
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { error = "Email and password are required" });
        }

        if (string.IsNullOrEmpty(request.CompanyName))
        {
            return BadRequest(new { error = "Company name is required" });
        }

        if (string.IsNullOrEmpty(request.PricingTierId))
        {
            return BadRequest(new { error = "Pricing tier is required" });
        }

        // Validate password strength
        if (request.Password.Length < 8)
        {
            return BadRequest(new { error = "Password must be at least 8 characters" });
        }

        // TODO: Check if email already exists
        // TODO: Create user in database
        // TODO: Create company/tenant in database
        // TODO: Create Stripe customer and subscription

        // Generate tenant ID
        var tenantId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var verificationToken = GenerateVerificationToken();

        // TODO: Store verification token in database with expiration

        // Send verification email
        try
        {
            await _emailService.SendVerificationEmailAsync(
                request.Email,
                request.FirstName ?? request.Email.Split('@')[0],
                verificationToken
            );
            _logger.LogInformation("Verification email sent to {Email}", request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", request.Email);
            // Continue with signup even if email fails - user can request resend
        }

        var response = new SignupResponse
        {
            UserId = userId,
            TenantId = tenantId,
            Email = request.Email,
            CompanyName = request.CompanyName,
            PricingTier = request.PricingTierId,
            TrialEndsAt = DateTime.UtcNow.AddDays(14),
            VerificationRequired = true,
            Message = "Account created successfully. Please check your email to verify your account."
        };

        _logger.LogInformation("Self-serve signup successful for {Email}, TenantId: {TenantId}",
            request.Email, tenantId);

        return Ok(response);
    }

    /// <summary>
    /// Verify email address
    /// </summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<ActionResult<VerifyEmailResponse>> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        _logger.LogInformation("Email verification attempt for token: {Token}", request.Token?[..8]);

        if (string.IsNullOrEmpty(request.Token))
        {
            return BadRequest(new { error = "Verification token is required" });
        }

        // TODO: Look up token in database
        // TODO: Mark email as verified
        // TODO: Generate JWT token for auto-login

        // Mock response
        return Ok(new VerifyEmailResponse
        {
            Verified = true,
            Message = "Email verified successfully. You can now log in.",
            RedirectUrl = "/onboarding"
        });
    }

    /// <summary>
    /// Resend verification email
    /// </summary>
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<ActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        _logger.LogInformation("Resend verification email for {Email}", request.Email);

        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { error = "Email is required" });
        }

        // TODO: Look up user by email to get first name
        // TODO: Generate new verification token and store in database

        var verificationToken = GenerateVerificationToken();
        var firstName = request.Email.Split('@')[0]; // TODO: Get from database

        // Send verification email
        try
        {
            await _emailService.SendVerificationEmailAsync(request.Email, firstName, verificationToken);
            _logger.LogInformation("Resent verification email to {Email}", request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend verification email to {Email}", request.Email);
            return StatusCode(500, new { error = "Failed to send verification email. Please try again later." });
        }

        return Ok(new { message = "Verification email sent. Please check your inbox." });
    }

    /// <summary>
    /// Check if email is available
    /// </summary>
    [HttpGet("check-email")]
    [AllowAnonymous]
    public async Task<ActionResult<CheckEmailResponse>> CheckEmail([FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new { error = "Email is required" });
        }

        // TODO: Check database for existing email

        return Ok(new CheckEmailResponse
        {
            Email = email,
            Available = true // Mock: always available for now
        });
    }

    /// <summary>
    /// Get onboarding status for current user
    /// </summary>
    [HttpGet("onboarding-status")]
    [Authorize]
    public async Task<ActionResult<OnboardingStatus>> GetOnboardingStatus()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new { error = "Tenant ID not found in token" });
        }

        // TODO: Get actual onboarding status from database

        return Ok(new OnboardingStatus
        {
            TenantId = tenantId,
            CompletedSteps = new List<string> { "signup", "verify_email" },
            CurrentStep = "create_ontology",
            TotalSteps = 5,
            PercentComplete = 40,
            Steps = new List<OnboardingStep>
            {
                new() { Id = "signup", Name = "Create Account", Completed = true },
                new() { Id = "verify_email", Name = "Verify Email", Completed = true },
                new() { Id = "create_ontology", Name = "Create Your First Ontology", Completed = false },
                new() { Id = "import_data", Name = "Import Data", Completed = false },
                new() { Id = "invite_team", Name = "Invite Team Members", Completed = false }
            }
        });
    }

    /// <summary>
    /// Update onboarding step completion
    /// </summary>
    [HttpPost("onboarding-step")]
    [Authorize]
    public async Task<ActionResult> CompleteOnboardingStep([FromBody] CompleteStepRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized(new { error = "Tenant ID not found in token" });
        }

        _logger.LogInformation("Completing onboarding step {Step} for tenant {TenantId}",
            request.StepId, tenantId);

        // TODO: Update step completion in database

        return Ok(new { message = $"Step '{request.StepId}' completed successfully" });
    }

    private static string GenerateVerificationToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}

#region DTOs

public class PricingTier
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Interval { get; set; } = "month";
    public string Description { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
    public TierLimits Limits { get; set; } = new();
    public string StripePriceId { get; set; } = string.Empty;
    public bool Popular { get; set; }
}

public class TierLimits
{
    public int Users { get; set; }
    public int Entities { get; set; }
    public int Integrations { get; set; }
    public int ApiCallsPerMonth { get; set; }
    public int StorageGb { get; set; }
}

public class SignupRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string PricingTierId { get; set; } = string.Empty;
    public string? ReferralCode { get; set; }
}

public class SignupResponse
{
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string PricingTier { get; set; } = string.Empty;
    public DateTime TrialEndsAt { get; set; }
    public bool VerificationRequired { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class VerifyEmailRequest
{
    public string Token { get; set; } = string.Empty;
}

public class VerifyEmailResponse
{
    public bool Verified { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
}

public class ResendVerificationRequest
{
    public string Email { get; set; } = string.Empty;
}

public class CheckEmailResponse
{
    public string Email { get; set; } = string.Empty;
    public bool Available { get; set; }
}

public class OnboardingStatus
{
    public string TenantId { get; set; } = string.Empty;
    public List<string> CompletedSteps { get; set; } = new();
    public string CurrentStep { get; set; } = string.Empty;
    public int TotalSteps { get; set; }
    public int PercentComplete { get; set; }
    public List<OnboardingStep> Steps { get; set; } = new();
}

public class OnboardingStep
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Completed { get; set; }
}

public class CompleteStepRequest
{
    public string StepId { get; set; } = string.Empty;
}

#endregion
