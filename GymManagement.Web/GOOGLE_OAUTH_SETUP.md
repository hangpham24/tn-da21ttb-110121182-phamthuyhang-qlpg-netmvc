# Hướng dẫn thiết lập Google OAuth

## 1. Tạo Google OAuth Client ID

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Chọn hoặc tạo một dự án mới
3. Đi đến **APIs & Services** > **Credentials**
4. Click **Create Credentials** > **OAuth client ID**
5. Chọn **Application type**: Web application
6. Đặt tên cho OAuth client (ví dụ: "Gym Management System")
7. Thêm **Authorized redirect URIs**:
   - Development: `https://localhost:7000/signin-google`
   - Production: `https://yourdomain.com/signin-google`
8. Click **Create**
9. Lưu lại **Client ID** và **Client Secret**

## 2. Cấu hình trong dự án

1. Mở file `appsettings.json`
2. Thay thế các giá trị placeholder:

```json
"Authentication": {
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  }
}
```

## 3. Cấu hình Google OAuth Consent Screen

1. Trong Google Cloud Console, đi đến **OAuth consent screen**
2. Chọn **User Type**: External (cho phép tất cả người dùng Google)
3. Điền thông tin:
   - App name: Gym Management System
   - User support email: (email của bạn)
   - App logo: (tùy chọn)
   - Application home page: URL của ứng dụng
   - Privacy policy link: (tùy chọn)
   - Terms of service link: (tùy chọn)
4. Thêm scopes:
   - `openid`
   - `email`
   - `profile`
5. Save và publish app

## 4. Test đăng nhập

1. Chạy ứng dụng: `dotnet run`
2. Truy cập trang đăng nhập
3. Click nút "Đăng nhập với Google"
4. Chọn tài khoản Google
5. Cho phép quyền truy cập
6. Hệ thống sẽ tự động tạo tài khoản và đăng nhập

## Lưu ý bảo mật

- **KHÔNG BAO GIỜ** commit Client Secret vào source control
- Sử dụng User Secrets cho development:
  ```bash
  dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
  dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
  ```
- Cho production, sử dụng environment variables hoặc Azure Key Vault

## Troubleshooting

### Lỗi "redirect_uri_mismatch"
- Kiểm tra Authorized redirect URIs trong Google Console
- Đảm bảo URL khớp chính xác (bao gồm protocol và port)

### Lỗi "Access blocked"
- Kiểm tra OAuth consent screen đã được publish chưa
- Đảm bảo app không ở trạng thái "Testing"

### Không lấy được thông tin email
- Kiểm tra scopes đã bao gồm "email" chưa
- User có thể đã ẩn email trong Google account settings
