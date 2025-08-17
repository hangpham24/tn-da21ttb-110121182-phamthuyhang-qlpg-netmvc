using Microsoft.EntityFrameworkCore;
using GymManagement.Web.Data;

namespace GymManagement.Tests.TestUtilities
{
    public class MockDbContext : GymDbContext
    {
        public MockDbContext() : base(new DbContextOptionsBuilder<GymDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
