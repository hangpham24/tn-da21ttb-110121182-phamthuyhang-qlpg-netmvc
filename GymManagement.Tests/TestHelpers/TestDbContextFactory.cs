using GymManagement.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagement.Tests.TestHelpers
{
    /// <summary>
    /// Factory để tạo In-Memory Database Context cho testing
    /// HOÀN TOÀN AN TOÀN - KHÔNG KẾT NỐI ĐẾN DATABASE THẬT
    /// </summary>
    public static class TestDbContextFactory
    {
        /// <summary>
        /// Tạo In-Memory Database Context với tên unique
        /// </summary>
        public static GymDbContext CreateInMemoryContext(string? databaseName = null)
        {
            databaseName ??= Guid.NewGuid().ToString();
            
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .EnableSensitiveDataLogging() // For testing only
                .Options;

            var context = new GymDbContext(options);
            
            // Ensure database is created
            context.Database.EnsureCreated();
            
            return context;
        }

        /// <summary>
        /// Tạo In-Memory Database Context với ServiceProvider
        /// </summary>
        public static GymDbContext CreateInMemoryContextWithServices(out IServiceProvider serviceProvider)
        {
            var services = new ServiceCollection();
            var databaseName = Guid.NewGuid().ToString();
            
            services.AddDbContext<GymDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: databaseName)
                       .EnableSensitiveDataLogging());
            
            serviceProvider = services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<GymDbContext>();
            
            context.Database.EnsureCreated();
            return context;
        }

        /// <summary>
        /// Kiểm tra context có phải In-Memory không (để đảm bảo an toàn)
        /// </summary>
        public static void EnsureInMemoryDatabase(GymDbContext context)
        {
            if (!context.Database.IsInMemory())
            {
                throw new InvalidOperationException(
                    "SECURITY ERROR: Test context is not using In-Memory database! " +
                    "This could affect production data.");
            }
        }

        /// <summary>
        /// Cleanup database sau test
        /// </summary>
        public static async Task CleanupDatabaseAsync(GymDbContext context)
        {
            EnsureInMemoryDatabase(context);
            
            // Clear all data
            context.DiemDanhs.RemoveRange(context.DiemDanhs);
            context.BangLuongs.RemoveRange(context.BangLuongs);
            context.DangKys.RemoveRange(context.DangKys);
            context.LopHocs.RemoveRange(context.LopHocs);
            context.NguoiDungs.RemoveRange(context.NguoiDungs);
            context.TaiKhoanVaiTros.RemoveRange(context.TaiKhoanVaiTros);
            context.TaiKhoans.RemoveRange(context.TaiKhoans);
            context.VaiTros.RemoveRange(context.VaiTros);
            
            await context.SaveChangesAsync();
        }
    }
}
