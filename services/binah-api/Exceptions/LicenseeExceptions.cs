namespace Binah.API.Exceptions;

/// <summary>
/// Exception thrown when a licensee is not found
/// </summary>
public class LicenseeNotFoundException : Exception
{
    public Guid LicenseeId { get; }

    public LicenseeNotFoundException(Guid licenseeId)
        : base($"Licensee with ID '{licenseeId}' was not found")
    {
        LicenseeId = licenseeId;
    }

    public LicenseeNotFoundException(Guid licenseeId, string message)
        : base(message)
    {
        LicenseeId = licenseeId;
    }

    public LicenseeNotFoundException(Guid licenseeId, string message, Exception innerException)
        : base(message, innerException)
    {
        LicenseeId = licenseeId;
    }
}

/// <summary>
/// Exception thrown when a license key is invalid or has expired
/// </summary>
public class InvalidLicenseKeyException : Exception
{
    public string LicenseKey { get; }

    public InvalidLicenseKeyException(string licenseKey)
        : base("License key is invalid or has expired")
    {
        // Don't log the actual key for security reasons
        LicenseKey = "***";
    }

    public InvalidLicenseKeyException(string licenseKey, string message)
        : base(message)
    {
        // Don't log the actual key for security reasons
        LicenseKey = "***";
    }

    public InvalidLicenseKeyException(string licenseKey, string message, Exception innerException)
        : base(message, innerException)
    {
        // Don't log the actual key for security reasons
        LicenseKey = "***";
    }
}

/// <summary>
/// Exception thrown when licensee context is required but not found in the request
/// </summary>
public class LicenseeContextMissingException : Exception
{
    public LicenseeContextMissingException()
        : base("Licensee context is required but was not found in the request")
    {
    }

    public LicenseeContextMissingException(string message)
        : base(message)
    {
    }

    public LicenseeContextMissingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a licensee's license has expired
/// </summary>
public class LicenseExpiredException : Exception
{
    public Guid LicenseeId { get; }
    public DateTime ExpiresAt { get; }

    public LicenseExpiredException(Guid licenseeId, DateTime expiresAt)
        : base($"License for licensee '{licenseeId}' expired on {expiresAt:yyyy-MM-dd}")
    {
        LicenseeId = licenseeId;
        ExpiresAt = expiresAt;
    }

    public LicenseExpiredException(Guid licenseeId, DateTime expiresAt, string message)
        : base(message)
    {
        LicenseeId = licenseeId;
        ExpiresAt = expiresAt;
    }
}

/// <summary>
/// Exception thrown when a licensee is suspended
/// </summary>
public class LicenseeSuspendedException : Exception
{
    public Guid LicenseeId { get; }

    public LicenseeSuspendedException(Guid licenseeId)
        : base($"Licensee '{licenseeId}' is suspended")
    {
        LicenseeId = licenseeId;
    }

    public LicenseeSuspendedException(Guid licenseeId, string message)
        : base(message)
    {
        LicenseeId = licenseeId;
    }
}

/// <summary>
/// Exception thrown when a feature is not enabled for a licensee
/// </summary>
public class FeatureNotEnabledException : Exception
{
    public string FeatureName { get; }
    public Guid LicenseeId { get; }

    public FeatureNotEnabledException(string featureName, Guid licenseeId)
        : base($"Feature '{featureName}' is not enabled for licensee '{licenseeId}'")
    {
        FeatureName = featureName;
        LicenseeId = licenseeId;
    }

    public FeatureNotEnabledException(string featureName, Guid licenseeId, string message)
        : base(message)
    {
        FeatureName = featureName;
        LicenseeId = licenseeId;
    }
}

/// <summary>
/// Exception thrown when a licensee exceeds their usage limits
/// </summary>
public class LicenseLimitExceededException : Exception
{
    public string LimitType { get; }
    public int Limit { get; }
    public int CurrentUsage { get; }

    public LicenseLimitExceededException(string limitType, int limit, int currentUsage)
        : base($"{limitType} limit exceeded: {currentUsage}/{limit}")
    {
        LimitType = limitType;
        Limit = limit;
        CurrentUsage = currentUsage;
    }

    public LicenseLimitExceededException(string limitType, int limit, int currentUsage, string message)
        : base(message)
    {
        LimitType = limitType;
        Limit = limit;
        CurrentUsage = currentUsage;
    }
}

/// <summary>
/// Exception thrown when a domain is not allowed for a licensee
/// </summary>
public class DomainNotAllowedException : Exception
{
    public string DomainId { get; }
    public Guid LicenseeId { get; }

    public DomainNotAllowedException(string domainId, Guid licenseeId)
        : base($"Domain '{domainId}' is not allowed for licensee '{licenseeId}'")
    {
        DomainId = domainId;
        LicenseeId = licenseeId;
    }

    public DomainNotAllowedException(string domainId, Guid licenseeId, string message)
        : base(message)
    {
        DomainId = domainId;
        LicenseeId = licenseeId;
    }
}
