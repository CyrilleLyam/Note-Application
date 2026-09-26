using Microsoft.Data.SqlClient;

namespace server.src.Config;

public static class DatabaseConfig
{
    public static string BuildConnectionString()
    {
        var host = EnvValidator.GetRequired("DB_HOST");
        var port = EnvValidator.GetOptional("DB_PORT");
        var username = EnvValidator.GetOptional("DB_USERNAME");

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = port == null ? host : $"{host},{port}",
            InitialCatalog = EnvValidator.GetRequired("DB_DATABASE"),
            TrustServerCertificate = EnvValidator.GetOptionalBool("DB_TRUST_SERVER_CERTIFICATE", true)
        };

        if (username == null)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = username;
            builder.Password = EnvValidator.GetRequired("DB_PASSWORD");
        }

        return builder.ConnectionString;
    }
}
