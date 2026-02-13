using IncidentManagementAPI.TenantData;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Data.Common;

namespace IncidentManagementAPI.Common
{
    public class TenantProvisioningService
    {
        private readonly TenantDbContextFactory _factory;

        public TenantProvisioningService(TenantDbContextFactory factory)
        {
            _factory = factory;
        }

        public async Task EnsureTenantDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            await using var db = _factory.Create(connectionString);

            await db.Database.OpenConnectionAsync(cancellationToken);
            try
            {
                var historyRepository = db.GetService<IHistoryRepository>();
                var historyExists = await historyRepository.ExistsAsync(cancellationToken);

                if (!historyExists)
                {
                    var connection = db.Database.GetDbConnection();

                    if (await TableExistsAsync(connection, "Projects", cancellationToken))
                    {
                        await EnsureHistoryTableAsync(connection, cancellationToken);

                        var migrationsAssembly = db.GetInfrastructure().GetRequiredService<IMigrationsAssembly>();
                        const string migratedProductVersion = "0";

                        foreach (var migrationId in migrationsAssembly.Migrations.Keys.OrderBy(id => id))
                        {
                            await InsertHistoryRowIfMissingAsync(connection, migrationId, migratedProductVersion, cancellationToken);
                        }

                        return;
                    }
                    else
                    {
                        await db.Database.MigrateAsync(cancellationToken);
                        return;
                    }
                }

                await db.Database.MigrateAsync(cancellationToken);
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }
        }

        private static async Task<bool> TableExistsAsync(DbConnection connection, string tableName, CancellationToken cancellationToken)
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = @tableName
  AND TABLE_SCHEMA = 'dbo'";

            var param = command.CreateParameter();
            param.ParameterName = "@tableName";
            param.Value = tableName;
            command.Parameters.Add(param);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }

        private static async Task EnsureHistoryTableAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            if (await TableExistsAsync(connection, "__EFMigrationsHistory", cancellationToken))
            {
                return;
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
CREATE TABLE __EFMigrationsHistory (
    MigrationId nvarchar(150) NOT NULL,
    ProductVersion nvarchar(32) NOT NULL,
    CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
)";

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task InsertHistoryRowIfMissingAsync(DbConnection connection, string migrationId, string productVersion, CancellationToken cancellationToken)
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = @migrationId)
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (@migrationId, @productVersion)
END";

            var idParam = command.CreateParameter();
            idParam.ParameterName = "@migrationId";
            idParam.Value = migrationId;
            command.Parameters.Add(idParam);

            var versionParam = command.CreateParameter();
            versionParam.ParameterName = "@productVersion";
            versionParam.Value = productVersion;
            command.Parameters.Add(versionParam);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

    }
}
