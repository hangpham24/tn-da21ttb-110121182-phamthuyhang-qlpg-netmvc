using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;

namespace GymManagement.Tests.TestHelpers
{
    /// <summary>
    /// Factory để tạo Mock Users cho testing
    /// HOÀN TOÀN AN TOÀN - KHÔNG TÁC ĐỘNG ĐẾN USER ACCOUNTS THẬT
    /// </summary>
    public static class MockUserFactory
    {
        /// <summary>
        /// Tạo Mock Admin User
        /// </summary>
        public static ClaimsPrincipal CreateMockAdmin(string userId = "admin-test-id", string username = "admin@test.com")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Tạo Mock Trainer User
        /// </summary>
        public static ClaimsPrincipal CreateMockTrainer(string userId = "trainer-test-id", string username = "trainer@test.com", int nguoiDungId = 1)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, username),
                new Claim(ClaimTypes.Role, "Trainer"),
                new Claim("NguoiDungId", nguoiDungId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Tạo Mock Customer User
        /// </summary>
        public static ClaimsPrincipal CreateMockCustomer(string userId = "customer-test-id", string username = "customer@test.com")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, username),
                new Claim(ClaimTypes.Role, "Customer")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Tạo Mock Unauthorized User (không có role)
        /// </summary>
        public static ClaimsPrincipal CreateMockUnauthorizedUser(string userId = "unauthorized-test-id", string username = "unauthorized@test.com")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, username)
                // Không có role claim
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Tạo Mock HttpContext với User
        /// </summary>
        public static Mock<HttpContext> CreateMockHttpContext(ClaimsPrincipal user)
        {
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(user);
            
            // Mock Session
            var mockSession = new Mock<ISession>();
            mockHttpContext.Setup(x => x.Session).Returns(mockSession.Object);
            
            return mockHttpContext;
        }

        /// <summary>
        /// Tạo Mock ControllerContext với User
        /// </summary>
        public static Microsoft.AspNetCore.Mvc.ControllerContext CreateMockControllerContext(ClaimsPrincipal user)
        {
            var mockHttpContext = CreateMockHttpContext(user);
            
            return new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };
        }

        /// <summary>
        /// Tạo nhiều Mock Trainers với IDs khác nhau
        /// </summary>
        public static List<ClaimsPrincipal> CreateMultipleTrainers(int count = 3)
        {
            var trainers = new List<ClaimsPrincipal>();
            
            for (int i = 1; i <= count; i++)
            {
                var trainer = CreateMockTrainer(
                    userId: $"trainer-{i}-test-id",
                    username: $"trainer{i}@test.com",
                    nguoiDungId: i
                );
                trainers.Add(trainer);
            }
            
            return trainers;
        }

        /// <summary>
        /// Validate Mock User (đảm bảo không phải real user)
        /// </summary>
        public static void ValidateMockUser(ClaimsPrincipal user)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            
            if (string.IsNullOrEmpty(userId) || !userId.Contains("test"))
            {
                throw new InvalidOperationException("SECURITY ERROR: User ID must contain 'test' for safety!");
            }
            
            if (string.IsNullOrEmpty(email) || !email.Contains("test.com"))
            {
                throw new InvalidOperationException("SECURITY ERROR: Email must be test email for safety!");
            }
        }
    }
}
