using Binah.API.Models;
using Npgsql;
using System.Text.Json;

namespace Binah.API.Repositories;

public class WhiteLabelConfigRepository : IWhiteLabelConfigRepository
{
    private readonly string _connectionString;
    private readonly ILogger<WhiteLabelConfigRepository> _logger;

    public WhiteLabelConfigRepository(IConfiguration configuration, ILogger<WhiteLabelConfigRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("PostgreSQL") ?? throw new InvalidOperationException("PostgreSQL connection string not configured");
        _logger = logger;
    }

    public async Task<WhiteLabelConfig?> GetByLicenseeIdAsync(Guid licenseeId)
    {
        const string sql = "SELECT * FROM whitelabel_configs WHERE licensee_id = @licensee_id";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("licensee_id", licenseeId);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapToConfig(reader);
        }

        return null;
    }

    public async Task<WhiteLabelConfig?> GetByCustomDomainAsync(string customDomain)
    {
        const string sql = "SELECT * FROM whitelabel_configs WHERE custom_domain = @custom_domain";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("custom_domain", customDomain);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapToConfig(reader);
        }

        return null;
    }

    public async Task<WhiteLabelConfig> CreateOrUpdateAsync(WhiteLabelConfig config)
    {
        const string sql = @"
            INSERT INTO whitelabel_configs (id, licensee_id, company_name, logo_url, favicon_url, custom_domain, colors, custom_text, fonts, created_at, updated_at)
            VALUES (@id, @licensee_id, @company_name, @logo_url, @favicon_url, @custom_domain, @colors, @custom_text, @fonts, @created_at, @updated_at)
            ON CONFLICT (licensee_id) DO UPDATE
            SET company_name = @company_name, logo_url = @logo_url, favicon_url = @favicon_url, custom_domain = @custom_domain,
                colors = @colors, custom_text = @custom_text, fonts = @fonts, updated_at = @updated_at
            RETURNING *";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        if (config.Id == Guid.Empty) config.Id = Guid.NewGuid();

        command.Parameters.AddWithValue("id", config.Id);
        command.Parameters.AddWithValue("licensee_id", config.LicenseeId);
        command.Parameters.AddWithValue("company_name", config.CompanyName);
        command.Parameters.AddWithValue("logo_url", (object?)config.LogoUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("favicon_url", (object?)config.FaviconUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("custom_domain", (object?)config.CustomDomain ?? DBNull.Value);
        command.Parameters.AddWithValue("colors", JsonSerializer.Serialize(config.Colors));
        command.Parameters.AddWithValue("custom_text", JsonSerializer.Serialize(config.CustomText));
        command.Parameters.AddWithValue("fonts", JsonSerializer.Serialize(config.Fonts));
        command.Parameters.AddWithValue("created_at", config.CreatedAt);
        command.Parameters.AddWithValue("updated_at", config.UpdatedAt);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapToConfig(reader);
        }

        throw new InvalidOperationException("Failed to create/update white-label config");
    }

    public async Task<bool> DeleteAsync(Guid licenseeId)
    {
        const string sql = "DELETE FROM whitelabel_configs WHERE licensee_id = @licensee_id";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("licensee_id", licenseeId);

        var rowsAffected = await command.ExecuteNonQueryAsync();

        return rowsAffected > 0;
    }

    private WhiteLabelConfig MapToConfig(NpgsqlDataReader reader)
    {
        var colorsJson = reader.GetString(reader.GetOrdinal("colors"));
        var customTextJson = reader.GetString(reader.GetOrdinal("custom_text"));
        var fontsJson = reader.GetString(reader.GetOrdinal("fonts"));

        return new WhiteLabelConfig
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            LicenseeId = reader.GetGuid(reader.GetOrdinal("licensee_id")),
            CompanyName = reader.GetString(reader.GetOrdinal("company_name")),
            LogoUrl = reader.IsDBNull(reader.GetOrdinal("logo_url")) ? null : reader.GetString(reader.GetOrdinal("logo_url")),
            FaviconUrl = reader.IsDBNull(reader.GetOrdinal("favicon_url")) ? null : reader.GetString(reader.GetOrdinal("favicon_url")),
            CustomDomain = reader.IsDBNull(reader.GetOrdinal("custom_domain")) ? null : reader.GetString(reader.GetOrdinal("custom_domain")),
            Colors = JsonSerializer.Deserialize<ThemeColors>(colorsJson) ?? ThemeColors.CreateDefault(),
            CustomText = JsonSerializer.Deserialize<Dictionary<string, string>>(customTextJson) ?? new(),
            Fonts = JsonSerializer.Deserialize<FontConfig>(fontsJson) ?? FontConfig.CreateDefault(),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }
}
