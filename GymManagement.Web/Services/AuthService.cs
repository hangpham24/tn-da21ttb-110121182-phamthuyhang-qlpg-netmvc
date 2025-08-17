using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace GymManagement.Web.Services
{
    public interface IAuthService
    {
        Task<TaiKhoan?> AuthenticateAsync(string username, string password);
        Task<TaiKhoan?> GetUserByIdAsync(string id);
        Task<TaiKhoan?> GetUserByEmailAsync(string email);
        Task<TaiKhoan?> GetUserByUsernameAsync(string username);
        Task<bool> CreateUserAsync(TaiKhoan user, string password, bool isGoogleUser = false);
        Task<bool> CreateAccountForExistingUserAsync(TaiKhoan user, string password);
        Task<bool> AssignRoleAsync(string userId, string roleName);
        Task<List<string>> GetUserRolesAsync(string userId);
        Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(TaiKhoan user);
        Task<bool> UpdateLastLoginAsync(string userId);
        Task<TaiKhoan?> FindByExternalLoginAsync(string provider, string providerKey);
        Task<bool> AddExternalLoginAsync(string userId, string provider, string providerKey, string? displayName = null);
        Task<bool> SaveExternalLoginAsync(string userId, string provider, string providerKey, string? displayName = null);
        Task<bool> ResetPasswordAsync(int nguoiDungId, string newPassword);
    }

    public class AuthService : IAuthService
    {
        private readonly GymDbContext _context;
        private readonly IPasswordService _passwordService;

        public AuthService(GymDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<TaiKhoan?> AuthenticateAsync(string usernameOrEmail, string password)
        {
            // Try to find user by username first, then by email
            var user = await GetUserByUsernameAsync(usernameOrEmail);

            // If not found by username, try by email
            if (user == null && IsValidEmail(usernameOrEmail))
            {
                user = await GetUserByEmailAsync(usernameOrEmail);
            }

            if (user == null || !user.KichHoat)
                return null;

            if (_passwordService.VerifyPassword(password, user.Salt, user.MatKhauHash))
            {
                await UpdateLastLoginAsync(user.Id);
                return user;
            }

            return null;
        }

        public async Task<TaiKhoan?> GetUserByIdAsync(string id)
        {
            return await _context.TaiKhoans
                .Include(u => u.NguoiDung)
                .Include(u => u.TaiKhoanVaiTros)
                    .ThenInclude(ur => ur.VaiTro)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<TaiKhoan?> GetUserByEmailAsync(string email)
        {
            return await _context.TaiKhoans
                .Include(u => u.NguoiDung)
                .Include(u => u.TaiKhoanVaiTros)
                    .ThenInclude(ur => ur.VaiTro)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<TaiKhoan?> GetUserByUsernameAsync(string username)
        {
            return await _context.TaiKhoans
                .Include(u => u.NguoiDung)
                .Include(u => u.TaiKhoanVaiTros)
                    .ThenInclude(ur => ur.VaiTro)
                .FirstOrDefaultAsync(u => u.TenDangNhap == username);
        }

        public async Task<bool> CreateUserAsync(TaiKhoan user, string password, bool isGoogleUser = false)
        {
            try
            {
                // Check if username or email already exists
                var existingUser = await _context.TaiKhoans
                    .FirstOrDefaultAsync(u => u.TenDangNhap == user.TenDangNhap || u.Email == user.Email);

                if (existingUser != null)
                    return false;

                // Hash password
                user.Salt = _passwordService.GenerateSalt();
                user.MatKhauHash = _passwordService.HashPassword(password, user.Salt);

                // Create NguoiDung record first
                var nguoiDung = new NguoiDung
                {
                    LoaiNguoiDung = "THANHVIEN",
                    Ho = isGoogleUser ? "Google User" : "Chưa cập nhật",
                    Ten = user.TenDangNhap,
                    Email = user.Email,
                    NgayThamGia = DateOnly.FromDateTime(DateTime.Now),
                    NgayTao = DateTime.UtcNow,
                    TrangThai = "ACTIVE"
                };

                _context.NguoiDungs.Add(nguoiDung);
                await _context.SaveChangesAsync();

                // Link TaiKhoan to NguoiDung
                user.NguoiDungId = nguoiDung.NguoiDungId;
                _context.TaiKhoans.Add(user);
                await _context.SaveChangesAsync();

                // Assign default Member role
                await AssignRoleAsync(user.Id, "Member");

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error creating user: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateAccountForExistingUserAsync(TaiKhoan user, string password)
        {
            try
            {
                // Check if username or email already exists
                var existingUser = await _context.TaiKhoans
                    .FirstOrDefaultAsync(u => u.TenDangNhap == user.TenDangNhap || u.Email == user.Email);

                if (existingUser != null)
                {
                    Console.WriteLine($"ERROR: Username '{user.TenDangNhap}' or email '{user.Email}' already exists");
                    Console.WriteLine($"Existing user ID: {existingUser.Id}, Username: {existingUser.TenDangNhap}, Email: {existingUser.Email}");
                    Console.WriteLine($"CONFLICT DETAILS:");
                    Console.WriteLine($"  - Requested Username: '{user.TenDangNhap}' vs Existing: '{existingUser.TenDangNhap}'");
                    Console.WriteLine($"  - Requested Email: '{user.Email}' vs Existing: '{existingUser.Email}'");
                    Console.WriteLine($"  - Existing Account Status: Active={existingUser.KichHoat}, EmailConfirmed={existingUser.EmailXacNhan}");
                    return false;
                }

                // Verify NguoiDung exists
                var nguoiDung = await _context.NguoiDungs.FindAsync(user.NguoiDungId);
                if (nguoiDung == null)
                {
                    Console.WriteLine($"ERROR: NguoiDung with ID {user.NguoiDungId} not found");
                    return false;
                }

                // Hash password
                user.Salt = _passwordService.GenerateSalt();
                user.MatKhauHash = _passwordService.HashPassword(password, user.Salt);

                // Add TaiKhoan only (NguoiDung already exists)
                _context.TaiKhoans.Add(user);
                await _context.SaveChangesAsync();

                Console.WriteLine($"SUCCESS: Created TaiKhoan for existing NguoiDung ID: {user.NguoiDungId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Exception in CreateAccountForExistingUserAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AssignRoleAsync(string userId, string roleName)
        {
            try
            {
                var role = await _context.VaiTros.FirstOrDefaultAsync(r => r.TenVaiTro == roleName);
                if (role == null)
                    return false;

                var existingAssignment = await _context.TaiKhoanVaiTros
                    .FirstOrDefaultAsync(ur => ur.TaiKhoanId == userId && ur.VaiTroId == role.Id);
                
                if (existingAssignment != null)
                    return true; // Already assigned

                var userRole = new TaiKhoanVaiTro
                {
                    TaiKhoanId = userId,
                    VaiTroId = role.Id
                };

                _context.TaiKhoanVaiTros.Add(userRole);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            return await _context.TaiKhoanVaiTros
                .Where(ur => ur.TaiKhoanId == userId)
                .Include(ur => ur.VaiTro)
                .Select(ur => ur.VaiTro.TenVaiTro)
                .ToListAsync();
        }

        public async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(TaiKhoan user)
        {
            var roles = await GetUserRolesAsync(user.Id);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.TenDangNhap),
                new Claim(ClaimTypes.Email, user.Email)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            if (user.NguoiDung != null)
            {
                claims.Add(new Claim("NguoiDungId", user.NguoiDung.NguoiDungId.ToString()));
                claims.Add(new Claim("HoTen", $"{user.NguoiDung.Ho} {user.NguoiDung.Ten}"));
            }
            else
            {
                // Try to find or create NguoiDung record for existing users
                var nguoiDung = await _context.NguoiDungs
                    .FirstOrDefaultAsync(n => n.Email == user.Email);

                if (nguoiDung == null)
                {
                    // Create missing NguoiDung record for existing user
                    nguoiDung = new NguoiDung
                    {
                        LoaiNguoiDung = "THANHVIEN",
                        Ho = "Chưa cập nhật",
                        Ten = user.TenDangNhap,
                        Email = user.Email,
                        NgayThamGia = DateOnly.FromDateTime(DateTime.Now),
                        NgayTao = DateTime.UtcNow,
                        TrangThai = "ACTIVE"
                    };

                    _context.NguoiDungs.Add(nguoiDung);
                    await _context.SaveChangesAsync();

                    // Update TaiKhoan to link to NguoiDung
                    user.NguoiDungId = nguoiDung.NguoiDungId;
                    _context.TaiKhoans.Update(user);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Link existing NguoiDung to TaiKhoan
                    user.NguoiDungId = nguoiDung.NguoiDungId;
                    _context.TaiKhoans.Update(user);
                    await _context.SaveChangesAsync();
                }

                claims.Add(new Claim("NguoiDungId", nguoiDung.NguoiDungId.ToString()));
                claims.Add(new Claim("HoTen", $"{nguoiDung.Ho} {nguoiDung.Ten}"));

                Console.WriteLine($"INFO: Created/linked NguoiDung for TaiKhoan {user.TenDangNhap} (ID: {user.Id})");
            }

            var identity = new ClaimsIdentity(claims, "Custom");
            return new ClaimsPrincipal(identity);
        }

        public async Task<bool> UpdateLastLoginAsync(string userId)
        {
            try
            {
                var user = await _context.TaiKhoans.FindAsync(userId);
                if (user != null)
                {
                    user.LanDangNhapCuoi = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<TaiKhoan?> FindByExternalLoginAsync(string provider, string providerKey)
        {
            var externalLogin = await _context.ExternalLogins
                .Include(el => el.TaiKhoan)
                    .ThenInclude(u => u.NguoiDung)
                .Include(el => el.TaiKhoan)
                    .ThenInclude(u => u.TaiKhoanVaiTros)
                        .ThenInclude(ur => ur.VaiTro)
                .FirstOrDefaultAsync(el => el.Provider == provider && el.ProviderKey == providerKey);

            return externalLogin?.TaiKhoan;
        }

        public async Task<bool> AddExternalLoginAsync(string userId, string provider, string providerKey, string? displayName = null)
        {
            try
            {
                var existingLogin = await _context.ExternalLogins
                    .FirstOrDefaultAsync(el => el.Provider == provider && el.ProviderKey == providerKey);
                
                if (existingLogin != null)
                    return false; // Already exists

                var externalLogin = new ExternalLogin
                {
                    TaiKhoanId = userId,
                    Provider = provider,
                    ProviderKey = providerKey,
                    ProviderDisplayName = displayName
                };

                _context.ExternalLogins.Add(externalLogin);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SaveExternalLoginAsync(string userId, string provider, string providerKey, string? displayName = null)
        {
            try
            {
                // Check if external login already exists
                var existingLogin = await _context.ExternalLogins
                    .FirstOrDefaultAsync(el => el.TaiKhoanId == userId && el.Provider == provider);
                
                if (existingLogin != null)
                {
                    // Update existing login
                    existingLogin.ProviderKey = providerKey;
                    existingLogin.ProviderDisplayName = displayName;
                    _context.ExternalLogins.Update(existingLogin);
                }
                else
                {
                    // Add new external login
                    var externalLogin = new ExternalLogin
                    {
                        TaiKhoanId = userId,
                        Provider = provider,
                        ProviderKey = providerKey,
                        ProviderDisplayName = displayName
                    };
                    _context.ExternalLogins.Add(externalLogin);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving external login: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(int nguoiDungId, string newPassword)
        {
            try
            {
                Console.WriteLine($"DEBUG: ResetPasswordAsync called for NguoiDungId: {nguoiDungId}");

                // Find TaiKhoan by NguoiDungId
                var taiKhoan = await _context.TaiKhoans
                    .FirstOrDefaultAsync(t => t.NguoiDungId == nguoiDungId);

                if (taiKhoan == null)
                {
                    Console.WriteLine($"ERROR: No TaiKhoan found for NguoiDungId: {nguoiDungId}");
                    return false;
                }

                Console.WriteLine($"DEBUG: Found TaiKhoan - ID: {taiKhoan.Id}, Username: {taiKhoan.TenDangNhap}");

                // Generate new salt and hash password
                var newSalt = _passwordService.GenerateSalt();
                var newHash = _passwordService.HashPassword(newPassword, newSalt);

                Console.WriteLine($"DEBUG: Generated new salt length: {newSalt.Length}, hash length: {newHash.Length}");

                taiKhoan.Salt = newSalt;
                taiKhoan.MatKhauHash = newHash;

                _context.TaiKhoans.Update(taiKhoan);
                await _context.SaveChangesAsync();

                Console.WriteLine($"SUCCESS: Password reset completed for TaiKhoan ID: {taiKhoan.Id}");

                // Verify the password was saved correctly
                var verifyResult = _passwordService.VerifyPassword(newPassword, taiKhoan.Salt, taiKhoan.MatKhauHash);
                Console.WriteLine($"DEBUG: Password verification after save: {verifyResult}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Exception in ResetPasswordAsync: {ex.Message}");
                Console.WriteLine($"ERROR: Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Check if string is a valid email format
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
