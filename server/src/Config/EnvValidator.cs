namespace server.src.Config;

public static class EnvValidator
{
    public static string GetRequired(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Environment variable '{key}' is required and cannot be empty.");
        }
        return value;
    }

    public static int GetRequiredInt(string key)
    {
        var value = GetRequired(key);
        if (!int.TryParse(value, out var result))
        {
            throw new InvalidOperationException($"Environment variable '{key}' must be a valid integer.");
        }
        return result;
    }

    public static string? GetOptional(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static bool GetOptionalBool(string key, bool defaultValue)
    {
        var value = GetOptional(key);
        if (value == null)
        {
            return defaultValue;
        }
        if (!bool.TryParse(value, out var result))
        {
            throw new InvalidOperationException($"Environment variable '{key}' must be 'true' or 'false'.");
        }
        return result;
    }

    public static void ValidateAll()
    {
        GetRequired("DB_HOST");
        GetRequired("DB_DATABASE");
        GetRequired("JWT_SECRET");
        GetRequired("JWT_ISSUER");
        GetRequired("JWT_AUDIENCE");
        GetRequiredInt("ACCESS_TOKEN_EXPIRATION_MINUTES");
        GetRequiredInt("REFRESH_TOKEN_EXPIRATION_DAYS");
        GetOptionalBool("DB_TRUST_SERVER_CERTIFICATE", true);

        if (GetOptional("DB_PORT") != null)
        {
            GetRequiredInt("DB_PORT");
        }

        if (GetOptional("DB_USERNAME") != null)
        {
            GetRequired("DB_PASSWORD");
        }
    }
}
