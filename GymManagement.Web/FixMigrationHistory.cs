using Microsoft.Data.SqlClient;
using System;

namespace GymManagement.Web
{
    /// <summary>
    /// Utility class to fix migration history when database exists but migration records are missing
    /// </summary>
    public class FixMigrationHistory
    {
        private static readonly string ConnectionString = 
            "Server=LAPTOP-4CEU6S6B\\DAIHOANGPHUC;Database=HANG_FIX;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        private static readonly string[] Migrations = new[]
        {
            "20250721151538_CustomAuthSystem",
            "20250723135839_AddMoTaAndThoiLuongToLopHoc",
            "20250724132844_FixDbContextConfiguration", 
            "20250812051928_AddMissingDiemDanhColumns",
            "20250813142951_RemoveLichLopAndKhuyenMaiUsage"
        };

        public static async Task Main(string[] args)
        {
            Console.WriteLine("🔧 Fixing Migration History for HANG_FIX Database...");
            
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                
                // Check if __EFMigrationsHistory exists
                var checkTableSql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__EFMigrationsHistory'";
                using var checkCmd = new SqlCommand(checkTableSql, connection);
                var tableExists = (int)await checkCmd.ExecuteScalarAsync() > 0;
                
                if (!tableExists)
                {
                    Console.WriteLine("📝 Creating __EFMigrationsHistory table...");
                    var createTableSql = @"
                        CREATE TABLE [__EFMigrationsHistory] (
                            [MigrationId] nvarchar(150) NOT NULL,
                            [ProductVersion] nvarchar(32) NOT NULL,
                            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                        );";
                    
                    using var createCmd = new SqlCommand(createTableSql, connection);
                    await createCmd.ExecuteNonQueryAsync();
                    Console.WriteLine("✅ __EFMigrationsHistory table created");
                }
                
                // Clear existing records
                Console.WriteLine("🧹 Clearing existing migration records...");
                using var clearCmd = new SqlCommand("DELETE FROM [__EFMigrationsHistory]", connection);
                await clearCmd.ExecuteNonQueryAsync();
                
                // Insert migration records
                Console.WriteLine("📝 Inserting migration records...");
                foreach (var migration in Migrations)
                {
                    var insertSql = "INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (@MigrationId, @ProductVersion)";
                    using var insertCmd = new SqlCommand(insertSql, connection);
                    insertCmd.Parameters.AddWithValue("@MigrationId", migration);
                    insertCmd.Parameters.AddWithValue("@ProductVersion", "8.0.0");
                    
                    await insertCmd.ExecuteNonQueryAsync();
                    Console.WriteLine($"  ✅ {migration}");
                }
                
                // Verify
                Console.WriteLine("\n🔍 Verifying migration history...");
                using var verifyCmd = new SqlCommand("SELECT [MigrationId], [ProductVersion] FROM [__EFMigrationsHistory] ORDER BY [MigrationId]", connection);
                using var reader = await verifyCmd.ExecuteReaderAsync();
                
                var count = 0;
                while (await reader.ReadAsync())
                {
                    count++;
                    Console.WriteLine($"  ✅ {reader["MigrationId"]} (v{reader["ProductVersion"]})");
                }
                
                Console.WriteLine($"\n✅ Migration history fixed successfully! ({count} migrations applied)");
                Console.WriteLine("🚀 You can now start the application with: dotnet run --urls \"http://localhost:5004\"");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine("💡 Try running the application anyway - it might work now.");
                Environment.Exit(1);
            }
        }
    }
}
