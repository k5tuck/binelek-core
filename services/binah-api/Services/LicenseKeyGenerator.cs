using System.Security.Cryptography;
using System.Text;

namespace Binah.API.Services;

/// <summary>
/// Generates and validates cryptographically secure license keys
/// Format: BINAH-{PREFIX}-{RANDOM}-{CHECKSUM}
/// Example: BINAH-ACME-8F3D9A2C-E5B74D1F
/// </summary>
public class LicenseKeyGenerator
{
    private readonly string _hmacSecret;

    public LicenseKeyGenerator(IConfiguration configuration)
    {
        _hmacSecret = configuration["License:HmacSecret"]
            ?? throw new InvalidOperationException("License:HmacSecret not configured");
    }

    /// <summary>
    /// Generate a new license key
    /// </summary>
    /// <param name="licenseePrefix">Short prefix for licensee (e.g., "ACME")</param>
    /// <returns>License key in format BINAH-{PREFIX}-{RANDOM}-{CHECKSUM}</returns>
    public string GenerateLicenseKey(string licenseePrefix)
    {
        if (string.IsNullOrWhiteSpace(licenseePrefix))
        {
            throw new ArgumentException("Licensee prefix cannot be empty", nameof(licenseePrefix));
        }

        // Sanitize prefix (uppercase, alphanumeric only, max 8 chars)
        var prefix = new string(licenseePrefix.ToUpperInvariant()
            .Where(c => char.IsLetterOrDigit(c))
            .Take(8)
            .ToArray());

        if (prefix.Length == 0)
        {
            prefix = "CUST";
        }

        // Generate random component (16 hex characters = 8 bytes)
        var randomBytes = new byte[8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        var randomPart = BitConverter.ToString(randomBytes).Replace("-", "");

        // Calculate checksum (8 hex characters = 4 bytes)
        var dataToHash = $"BINAH-{prefix}-{randomPart}";
        var checksumBytes = CalculateChecksum(dataToHash);
        var checksum = BitConverter.ToString(checksumBytes).Replace("-", "");

        // Construct final key: BINAH-{PREFIX}-{RANDOM}-{CHECKSUM}
        return $"BINAH-{prefix}-{randomPart}-{checksum}";
    }

    /// <summary>
    /// Validate a license key format and checksum
    /// </summary>
    public bool ValidateLicenseKeyFormat(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return false;
        }

        // Expected format: BINAH-{PREFIX}-{RANDOM}-{CHECKSUM}
        var parts = licenseKey.Split('-');

        if (parts.Length != 4)
        {
            return false;
        }

        if (parts[0] != "BINAH")
        {
            return false;
        }

        // Validate random part (16 hex characters)
        if (parts[2].Length != 16 || !IsHexString(parts[2]))
        {
            return false;
        }

        // Validate checksum part (8 hex characters)
        if (parts[3].Length != 8 || !IsHexString(parts[3]))
        {
            return false;
        }

        // Verify checksum
        var dataToHash = $"{parts[0]}-{parts[1]}-{parts[2]}";
        var expectedChecksum = BitConverter.ToString(CalculateChecksum(dataToHash)).Replace("-", "");

        return string.Equals(parts[3], expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Hash a license key for storage (HMAC-SHA256)
    /// </summary>
    public string HashLicenseKey(string licenseKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(licenseKey));
        return Convert.ToBase64String(hash);
    }

    private byte[] CalculateChecksum(string data)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
        // Take first 4 bytes (8 hex characters)
        return hash.Take(4).ToArray();
    }

    private bool IsHexString(string str)
    {
        return str.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
    }
}
