# User Session Management System

## Tổng quan

Hệ thống quản lý session người dùng được thiết kế để giải quyết các vấn đề về việc truy xuất thông tin người dùng trong ứng dụng ASP.NET Core, bao gồm:

- Session timeout và mất thông tin người dùng
- Claims không được lưu đúng hoặc bị mất
- Lỗi khi truy vấn database liên tục
- Thiếu error handling và logging

## Kiến trúc

### 1. UserSessionService
- **Interface**: `IUserSessionService`
- **Implementation**: `UserSessionService`
- **Chức năng**: Quản lý thông tin người dùng trong session, cache và refresh claims

### 2. BaseController
- **Chức năng**: Base class cho tất cả controllers với error handling tự động
- **Features**: Safe methods để truy xuất thông tin người dùng, logging, error handling

### 3. Middleware
- **UserSessionMiddleware**: Validate và refresh user session
- **GlobalExceptionMiddleware**: Handle exceptions toàn cục

## Cách sử dụng

### 1. Controller kế thừa từ BaseController

```csharp
public class MyController : BaseController
{
    public MyController(IUserSessionService userSessionService, ILogger<MyController> logger) 
        : base(userSessionService, logger)
    {
    }

    public async Task<IActionResult> MyAction()
    {
        try
        {
            // Get current user safely
            var currentUser = await GetCurrentUserSafeAsync();
            if (currentUser == null)
            {
                return HandleUserNotFound("MyAction");
            }

            // Check role safely
            if (!IsInRoleSafe("Admin"))
            {
                return HandleUnauthorized();
            }

            // Get user ID safely
            var userId = GetCurrentUserIdSafe();
            var nguoiDungId = GetCurrentNguoiDungIdSafe();

            // Log user action
            LogUserAction("MyAction", new { userId, nguoiDungId });

            // Your business logic here
            
            return View();
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Có lỗi xảy ra khi thực hiện hành động.");
        }
    }
}
```

### 2. Sử dụng UserSessionService trực tiếp

```csharp
public class MyService
{
    private readonly IUserSessionService _userSessionService;

    public MyService(IUserSessionService userSessionService)
    {
        _userSessionService = userSessionService;
    }

    public async Task<bool> DoSomething()
    {
        var currentUser = await _userSessionService.GetCurrentUserAsync();
        if (currentUser == null)
        {
            return false;
        }

        var isAdmin = _userSessionService.IsInRole("Admin");
        // Business logic
        
        return true;
    }
}
```

### 3. AJAX Endpoints với Validation

```csharp
[ValidateUserSession("Admin")]
public async Task<IActionResult> ApiEndpoint()
{
    // User session is automatically validated
    var currentUser = await GetCurrentUserSafeAsync();
    
    return Json(new { success = true, data = currentUser });
}
```

### 4. Manual Session Management

```csharp
// Set user session after login
await _userSessionService.SetCurrentUserAsync(user);

// Update session after profile changes
await _userSessionService.UpdateCurrentUserAsync(updatedUser);

// Refresh claims and session
await _userSessionService.RefreshUserClaimsAsync();

// Clear session on logout
await _userSessionService.ClearCurrentUserAsync();
```

## API Reference

### IUserSessionService Methods

#### GetCurrentUserAsync()
```csharp
Task<UserSessionInfo?> GetCurrentUserAsync()
```
Lấy thông tin người dùng hiện tại từ session. Tự động rebuild nếu session không tồn tại.

#### SetCurrentUserAsync(TaiKhoan user)
```csharp
Task SetCurrentUserAsync(TaiKhoan user)
```
Lưu thông tin người dùng vào session.

#### RefreshUserClaimsAsync()
```csharp
Task RefreshUserClaimsAsync()
```
Refresh claims và session từ database.

#### ClearCurrentUserAsync()
```csharp
Task ClearCurrentUserAsync()
```
Xóa session người dùng.

#### Safe Methods
```csharp
bool IsUserAuthenticated()
string? GetUserId()
string? GetUserName()
string? GetUserEmail()
int? GetNguoiDungId()
string? GetFullName()
List<string> GetUserRoles()
bool IsInRole(string role)
```

### BaseController Methods

#### GetCurrentUserSafeAsync()
```csharp
protected async Task<UserSessionInfo?> GetCurrentUserSafeAsync()
```
Lấy thông tin người dùng với error handling.

#### Safe Helper Methods
```csharp
protected string? GetCurrentUserIdSafe()
protected int? GetCurrentNguoiDungIdSafe()
protected bool IsInRoleSafe(string role)
```

#### Error Handling Methods
```csharp
protected IActionResult HandleUserNotFound(string? action = null)
protected IActionResult HandleUnauthorized(string message = "...")
protected IActionResult HandleError(Exception ex, string userMessage = "...")
```

#### Logging
```csharp
protected void LogUserAction(string action, object? data = null)
```

## Configuration

### Program.cs Setup

```csharp
// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserSessionService, UserSessionService>();

// Configure middleware pipeline
app.UseSession();
app.UseAuthentication();
app.UseUserSession(); // Custom middleware
app.UseAuthorization();
app.UseGlobalExceptionHandling(); // Global error handling
```

## Error Handling

### Automatic Error Handling
- **BaseController**: Tự động handle lỗi trong OnActionExecutionAsync
- **GlobalExceptionMiddleware**: Handle lỗi toàn cục
- **UserSessionMiddleware**: Validate session và redirect khi cần

### Custom Error Responses
- **AJAX Requests**: Trả về JSON với error message
- **Regular Requests**: Redirect đến error page hoặc login page

### Logging
- Tất cả lỗi được log với context đầy đủ
- User actions được log để audit trail
- Session events được log để debugging

## Best Practices

### 1. Controller Design
- Luôn kế thừa từ `BaseController`
- Sử dụng safe methods để truy xuất user info
- Handle exceptions với `HandleError()`
- Log user actions quan trọng

### 2. Session Management
- Refresh session sau khi update profile
- Clear session khi logout
- Validate session trong middleware

### 3. Error Handling
- Sử dụng user-friendly error messages
- Log chi tiết lỗi cho debugging
- Redirect appropriately dựa trên context

### 4. Security
- Validate user roles trước khi thực hiện actions
- Clear session khi detect security issues
- Log security-related events

## Troubleshooting

### Common Issues

1. **"Không thể lấy thông tin người dùng"**
   - Check session configuration
   - Verify middleware order
   - Check database connection

2. **Session bị mất sau một thời gian**
   - Check session timeout configuration
   - Verify cookie settings
   - Check if session store is working

3. **Claims không được update**
   - Call `RefreshUserClaimsAsync()` after changes
   - Check if user exists in database
   - Verify role assignments

### Debug Tips

1. Enable detailed logging:
```csharp
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

2. Check session in browser dev tools
3. Monitor application logs for session-related events
4. Use breakpoints in middleware to debug flow

## Migration Guide

### From Old System
1. Replace direct `User.FindFirst()` calls with safe methods
2. Update controllers to inherit from `BaseController`
3. Add session configuration to `Program.cs`
4. Update login/logout logic to use `UserSessionService`
5. Test all user-related functionality

### Testing
1. Test login/logout flow
2. Test session timeout scenarios
3. Test role-based access
4. Test error handling scenarios
5. Test concurrent user sessions
