using Microsoft.Data.SqlClient;

namespace IncidentManagementAPI.Common
{
    public static class TenantConnectionStringBuilder
    {
        public static string BuildForTenant(string baseConnectionString, string tenantKey)
        {
            var dbName = $"IncidentsTenant_{tenantKey.Trim().ToLowerInvariant()}";

            var builder = new SqlConnectionStringBuilder(baseConnectionString ?? string.Empty)
            {
                InitialCatalog = dbName
            };

            return builder.ConnectionString;
        }
    }
}
