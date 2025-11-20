using Binah.API.Models;
using Npgsql;
using System.Text.Json;

namespace Binah.API.Repositories;

/// <summary>
/// PostgreSQL implementation of licensee repository
/// </summary>
public class LicenseeRepository : ILicenseeRepository
{
    private readonly string _connectionString;
    private readonly ILogger<LicenseeRepository> _logger;

    public LicenseeRepository(
        IConfiguration configuration,
        ILogger<LicenseeRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("PostgreSQL connection string not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Licensee?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id, name, license_key_hash, status, features, created_at, expires_at,
                   contact_email, contact_name, company_name, metadata
            FROM licensees
            WHERE id = @id";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapToLicensee(reader);
        }

        return null;
    }

    public async Task<Licensee?> GetByLicenseKeyHashAsync(string licenseKeyHash)
    {
        const string sql = @"
            SELECT id, name, license_key_hash, status, features, created_at, expires_at,
                   contact_email, contact_name, company_name, metadata
            FROM licensees
            WHERE license_key_hash = @license_key_hash";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("license_key_hash", licenseKeyHash);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapToLicensee(reader);
        }

        return null;
    }

    public async Task<List<Licensee>> GetAllAsync()
    {
        const string sql = @"
            SELECT id, name, license_key_hash, status, features, created_at, expires_at,
                   contact_email, contact_name, company_name, metadata
            FROM licensees
            ORDER BY created_at DESC";

        var licensees = new List<Licensee>();

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            licensees.Add(MapToLicensee(reader));
        }

        return licensees;
    }

    public async Task<List<Licensee>> GetByStatusAsync(LicenseeStatus status)
    {
        const string sql = @"
            SELECT id, name, license_key_hash, status, features, created_at, expires_at,
                   contact_email, contact_name, company_name, metadata
            FROM licensees
            WHERE status = @status
            ORDER BY created_at DESC";

        var licensees = new List<Licensee>();

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("status", status.ToString().ToLowerInvariant());

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            licensees.Add(MapToLicensee(reader));
        }

        return licensees;
    }

    public async Task<Licensee> CreateAsync(Licensee licensee)
    {
        const string sql = @"
            INSERT INTO licensees (id, name, license_key_hash, status, features, created_at, expires_at,
                                   contact_email, contact_name, company_name, metadata)
            VALUES (@id, @name, @license_key_hash, @status, @features, @created_at, @expires_at,
                    @contact_email, @contact_name, @company_name, @metadata)
            RETURNING id, name, license_key_hash, status, features, created_at, expires_at,
                      contact_email, contact_name, company_name, metadata";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        AddLicenseeParameters(command, licensee);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapToLicensee(reader);
        }

        throw new InvalidOperationException("Failed to create licensee");
    }

    public async Task<Licensee> UpdateAsync(Licensee licensee)
    {
        const string sql = @"
            UPDATE licensees
            SET name = @name,
                license_key_hash = @license_key_hash,
                status = @status,
                features = @features,
                expires_at = @expires_at,
                contact_email = @contact_email,
                contact_name = @contact_name,
                company_name = @company_name,
                metadata = @metadata
            WHERE id = @id
            RETURNING id, name, license_key_hash, status, features, created_at, expires_at,
                      contact_email, contact_name, company_name, metadata";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        AddLicenseeParameters(command, licensee);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapToLicensee(reader);
        }

        throw new InvalidOperationException($"Failed to update licensee {licensee.Id}");
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM licensees WHERE id = @id";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        var rowsAffected = await command.ExecuteNonQueryAsync();

        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        const string sql = "SELECT COUNT(1) FROM licensees WHERE id = @id";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);

        return count > 0;
    }

    /// <summary>
    /// Map database row to Licensee object
    /// </summary>
    private Licensee MapToLicensee(NpgsqlDataReader reader)
    {
        var featuresJson = reader.GetString(reader.GetOrdinal("features"));
        var metadataJson = reader.GetString(reader.GetOrdinal("metadata"));

        var features = JsonSerializer.Deserialize<LicenseFeatures>(featuresJson)
            ?? new LicenseFeatures();

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson)
            ?? new Dictionary<string, object>();

        return new Licensee
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            LicenseKeyHash = reader.GetString(reader.GetOrdinal("license_key_hash")),
            Status = Enum.Parse<LicenseeStatus>(reader.GetString(reader.GetOrdinal("status")), true),
            Features = features,
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            ExpiresAt = reader.IsDBNull(reader.GetOrdinal("expires_at"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("expires_at")),
            ContactEmail = reader.IsDBNull(reader.GetOrdinal("contact_email"))
                ? null
                : reader.GetString(reader.GetOrdinal("contact_email")),
            ContactName = reader.IsDBNull(reader.GetOrdinal("contact_name"))
                ? null
                : reader.GetString(reader.GetOrdinal("contact_name")),
            CompanyName = reader.IsDBNull(reader.GetOrdinal("company_name"))
                ? null
                : reader.GetString(reader.GetOrdinal("company_name")),
            Metadata = metadata
        };
    }

    /// <summary>
    /// Add licensee parameters to command
    /// </summary>
    private void AddLicenseeParameters(NpgsqlCommand command, Licensee licensee)
    {
        command.Parameters.AddWithValue("id", licensee.Id);
        command.Parameters.AddWithValue("name", licensee.Name);
        command.Parameters.AddWithValue("license_key_hash", licensee.LicenseKeyHash);
        command.Parameters.AddWithValue("status", licensee.Status.ToString().ToLowerInvariant());
        command.Parameters.AddWithValue("features", JsonSerializer.Serialize(licensee.Features));
        command.Parameters.AddWithValue("created_at", licensee.CreatedAt);
        command.Parameters.AddWithValue("expires_at", (object?)licensee.ExpiresAt ?? DBNull.Value);
        command.Parameters.AddWithValue("contact_email", (object?)licensee.ContactEmail ?? DBNull.Value);
        command.Parameters.AddWithValue("contact_name", (object?)licensee.ContactName ?? DBNull.Value);
        command.Parameters.AddWithValue("company_name", (object?)licensee.CompanyName ?? DBNull.Value);
        command.Parameters.AddWithValue("metadata", JsonSerializer.Serialize(licensee.Metadata));
    }
}
