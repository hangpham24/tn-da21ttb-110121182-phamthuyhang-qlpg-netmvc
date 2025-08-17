# Test Script cho Booking Search Dropdowns

## 🎯 **Các cải thiện đã thực hiện:**

### **1. Searchable Member Dropdown**
- **Search functionality:** Gõ để tìm kiếm thành viên
- **Filter user types:** Chỉ hiển thị THANHVIEN và VANGLAI
- **Visual indicators:** Icons phân biệt loại người dùng
- **Click to select:** Click để chọn thành viên

### **2. Searchable Class Dropdown**
- **Search functionality:** Gõ để tìm kiếm lớp học
- **Real-time filter:** Filter ngay khi gõ
- **Auto-load info:** Tự động load thông tin lớp khi chọn
- **Click to select:** Click để chọn lớp học

### **3. Fixed Class Info Loading**
- **Updated references:** Sử dụng `selectedClassId` thay vì `LopHocId`
- **Auto-trigger:** Tự động load info khi chọn lớp từ dropdown
- **No more "Đang tải...":** Thông tin được cập nhật ngay

## 🧪 **Test Cases:**

### **Test Case 1: Member Search Dropdown**
1. **Truy cập:** http://localhost:5003/Booking/Create (với quyền Admin)
2. **Click vào field "Thành viên":**
   - ✅ Dropdown hiển thị với header "Chọn thành viên hoặc khách vãng lai"
   - ✅ Chỉ hiển thị THANHVIEN (👤) và VANGLAI (🚶)
   - ✅ Không hiển thị HLV hoặc ADMIN
3. **Test search:**
   - Gõ "Phạm" → Chỉ hiển thị users có "Phạm" trong tên
   - Gõ "vãng lai" → Chỉ hiển thị users VANGLAI
   - Clear search → Hiển thị lại tất cả
4. **Test selection:**
   - Click vào một user → Field hiển thị tên đầy đủ
   - Hidden input `selectedMemberId` có value đúng

### **Test Case 2: Class Search Dropdown**
1. **Click vào field "Lớp học":**
   - ✅ Dropdown hiển thị với header "Chọn lớp học để đặt lịch"
   - ✅ Hiển thị tất cả lớp học active
2. **Test search:**
   - Gõ "Yoga" → Chỉ hiển thị lớp có "Yoga" trong tên
   - Gõ "buổi sáng" → Filter theo từ khóa
   - Clear search → Hiển thị lại tất cả
3. **Test selection:**
   - Click vào một lớp → Field hiển thị tên lớp
   - Hidden input `selectedClassId` có value đúng
   - **Thông tin lớp học tự động load** (không còn "Đang tải...")

### **Test Case 3: Class Info Auto-Loading**
1. **Chọn lớp học từ dropdown**
2. **Check phần "ℹ️ Thông tin lớp học":**
   - ✅ ID lớp: Hiển thị ngay
   - ✅ Thời gian: Hiển thị ngay (không còn "Đang tải...")
   - ✅ Huấn luyện viên: Hiển thị ngay
   - ✅ Sức chứa: Hiển thị ngay
3. **Chọn ngày:**
   - ✅ Thông tin availability cập nhật
   - ✅ Submit button enable khi đủ thông tin

### **Test Case 4: Dropdown Behavior**
1. **Click outside dropdown:** Dropdown tự động đóng
2. **Focus vào search field:** Dropdown tự động mở
3. **Keyboard navigation:** Có thể dùng phím mũi tên (nếu implement)
4. **Mobile responsive:** Hoạt động tốt trên mobile

### **Test Case 5: Form Submission**
1. **Chọn thành viên từ search dropdown**
2. **Chọn lớp học từ search dropdown**
3. **Chọn ngày**
4. **Submit form**
5. **Expected:** Booking được tạo thành công với đúng thông tin

## 🎨 **UI/UX Improvements:**

### **Search Field Design:**
```
🔍 Tìm kiếm thành viên...
🔍 Tìm kiếm lớp học...
```

### **Dropdown Structure:**
```
┌─────────────────────────────────────┐
│ Chọn thành viên hoặc khách vãng lai │ ← Header
├─────────────────────────────────────┤
│ 👤 Phạm Thúy Hằng (Thành viên)     │ ← Option
│ 🚶 Nguyễn Văn A (Vãng lai)         │ ← Option
│ 👤 Trần Thị B (Thành viên)         │ ← Option
└─────────────────────────────────────┘
```

### **Visual States:**
- **Hover:** Background xanh nhạt
- **Selected:** Field hiển thị text đã chọn
- **Focus:** Border xanh, dropdown mở
- **Search:** Real-time filtering

## 🔍 **Technical Details:**

### **Filter Logic (Members):**
```javascript
.Where(u => u.TrangThai == "ACTIVE" && 
           (u.LoaiNguoiDung == "THANHVIEN" || u.LoaiNguoiDung == "VANGLAI"))
```

### **Search Implementation:**
```javascript
memberSearch.addEventListener('input', (e) => {
    const searchTerm = e.target.value.toLowerCase();
    memberOptions.forEach(option => {
        const text = option.textContent.toLowerCase();
        if (text.includes(searchTerm)) {
            option.style.display = 'block';
        } else {
            option.style.display = 'none';
        }
    });
});
```

### **Auto-Load Class Info:**
```javascript
classOptions.forEach(option => {
    option.addEventListener('click', () => {
        // ... set values
        loadClassInfo(option.dataset.value); // ← Auto-trigger
        updateSubmitButton();
    });
});
```

## 🎯 **Expected Results:**

1. **Member Dropdown:** Chỉ THANHVIEN và VANGLAI, có search
2. **Class Dropdown:** Tất cả lớp học, có search
3. **Real-time Search:** Filter ngay khi gõ
4. **Auto-load Info:** Thông tin lớp học load ngay khi chọn
5. **No "Đang tải...":** Thông tin hiển thị ngay lập tức
6. **Smooth UX:** Dropdown đóng/mở mượt mà
7. **Form Works:** Submit thành công với đúng data

## 🚨 **Potential Issues:**

### **Issue 1: Dropdown không hiển thị**
- **Check:** CSS z-index có đúng không
- **Check:** JavaScript có lỗi không

### **Issue 2: Search không hoạt động**
- **Check:** Event listeners có được attach không
- **Check:** Query selectors có đúng class/id không

### **Issue 3: Class info vẫn "Đang tải..."**
- **Check:** `loadClassInfo()` có được gọi không
- **Check:** `selectedClassId` có value đúng không

### **Issue 4: Form submission lỗi**
- **Check:** Hidden inputs có values đúng không
- **Check:** Server có nhận được đúng data không

## 💡 **Performance Notes:**

- **Client-side search:** Nhanh, không cần server requests
- **Minimal DOM manipulation:** Chỉ show/hide elements
- **Event delegation:** Efficient event handling
- **Lazy loading:** Dropdown chỉ mở khi cần

## 🔧 **Files Modified:**

1. **Controllers/BookingController.cs:**
   - Updated member filter: chỉ THANHVIEN và VANGLAI
   - Removed HLV và ADMIN từ dropdown

2. **Views/Booking/Create.cshtml:**
   - Replaced select dropdowns với searchable inputs
   - Added dropdown containers với search functionality
   - Updated JavaScript để handle search và selection
   - Fixed class info loading references
