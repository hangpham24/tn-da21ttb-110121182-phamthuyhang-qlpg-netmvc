# Sửa lỗi redirect_uri_mismatch cho Google OAuth

## Nguyên nhân
Ứng dụng đang chạy với HTTP (http://localhost:5003) nhưng Google Console được cấu hình với HTTPS (https://localhost:5003).

## Giải pháp 1: Thêm HTTP URI vào Google Console (Khuyến nghị cho Development)

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Chọn project của bạn
3. Đi đến **APIs & Services** > **Credentials**
4. Click vào OAuth 2.0 Client ID của bạn (Gym Fitness)
5. Trong phần **Authorized redirect URIs**, thêm URI mới:
   ```
   http://localhost:5003/signin-google
   ```
6. Click **Save**
7. Đợi 5-10 phút để cập nhật có hiệu lực

## Giải pháp 2: Chạy ứng dụng với HTTPS

### Cách 1: Sử dụng launchSettings.json
1. Mở file `Properties/launchSettings.json`
2. Tìm profile "https" và chạy với profile đó:
   ```bash
   dotnet run --launch-profile https
   ```

### Cách 2: Cấu hình HTTPS trong Program.cs
Thêm vào Program.cs trước `app.Run()`:

```csharp
// Force HTTPS in development
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
```

### Cách 3: Đổi port mặc định
Mở file `Properties/launchSettings.json` và thay đổi:

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:5003",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project", 
      "applicationUrl": "https://localhost:7000;http://localhost:5003",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## Giải pháp tạm thời cho Development

Trong `appsettings.Development.json`, bạn có thể override callback path:

```json
{
  "Authentication": {
    "Google": {
      "CallbackPath": "/signin-google"
    }
  }
}
```

## Kiểm tra lại

1. Đảm bảo redirect URI trong code khớp với Google Console
2. Clear browser cache và cookies
3. Thử đăng nhập lại

## Lưu ý
- Cho production, LUÔN sử dụng HTTPS
- Google OAuth không cho phép HTTP cho production domains
- Localhost được phép dùng HTTP cho development
