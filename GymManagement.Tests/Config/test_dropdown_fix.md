# Test Script cho Dropdown Selection Fix

## 🎯 **Vấn đề đã được fix:**

### **Root Cause:**
- Complex HTML structure với nested divs
- `textContent` lấy tất cả text bao gồm whitespace
- Event listeners có thể bị conflict

### **Solutions Applied:**
1. **Use `dataset.text` thay vì `textContent`** cho search và selection
2. **Added fallback onclick handlers** nếu addEventListener fails
3. **Improved click outside logic** với proper container detection
4. **Added console logs** để debug

---

## 🧪 **Test Cases:**

### **Test Case 1: Member Dropdown Selection**
1. **Truy cập:** http://localhost:5003/Booking/Create (Admin)
2. **Mở Developer Tools** (F12) → Console tab
3. **Click vào Member search field:**
   - ✅ Dropdown mở
   - ✅ Hiển thị danh sách members với avatars
4. **Click vào một member option:**
   - ✅ Console log: `Member clicked: [name] [id]` hoặc `Fallback member click: [name]`
   - ✅ Input field hiển thị tên đầy đủ
   - ✅ Dropdown đóng
   - ✅ Hidden field `selectedMemberId` có value

### **Test Case 2: Class Dropdown Selection**
1. **Click vào Class search field:**
   - ✅ Dropdown mở với green theme
   - ✅ Hiển thị danh sách classes với running icons
2. **Click vào một class option:**
   - ✅ Console log: `Class clicked: [name] [id]` hoặc `Fallback class click: [name]`
   - ✅ Input field hiển thị tên lớp học
   - ✅ Dropdown đóng
   - ✅ Hidden field `selectedClassId` có value
   - ✅ Class info loads ngay lập tức

### **Test Case 3: Search Functionality**
1. **Member search:**
   - Gõ "Phạm" → Filter options chứa "Phạm"
   - Clear search → Hiển thị lại tất cả
   - Click filtered option → Selection works
2. **Class search:**
   - Gõ "Yoga" → Filter options chứa "Yoga"
   - Clear search → Hiển thị lại tất cả
   - Click filtered option → Selection works + class info loads

### **Test Case 4: Click Outside Behavior**
1. **Open member dropdown** → Click outside → Dropdown closes
2. **Open class dropdown** → Click outside → Dropdown closes
3. **Open dropdown** → Click on other elements → Dropdown closes properly

### **Test Case 5: Complete Form Flow**
1. **Select member** → Field updates, hidden field has value
2. **Select class** → Field updates, class info loads, hidden field has value
3. **Select date** → Availability check runs
4. **Submit form** → All values are properly submitted

---

## 🔍 **Debug Commands:**

### **Check if elements exist:**
```javascript
console.log('Elements check:', {
    memberSearch: !!document.getElementById('memberSearch'),
    memberDropdown: !!document.getElementById('memberDropdown'),
    selectedMemberId: !!document.getElementById('selectedMemberId'),
    classSearch: !!document.getElementById('classSearch'),
    classDropdown: !!document.getElementById('classDropdown'),
    selectedClassId: !!document.getElementById('selectedClassId')
});
```

### **Check options data:**
```javascript
document.querySelectorAll('.member-option').forEach((option, i) => {
    console.log(`Member ${i}:`, {
        text: option.dataset.text,
        value: option.dataset.value,
        hasClick: !!option.onclick
    });
});

document.querySelectorAll('.class-option').forEach((option, i) => {
    console.log(`Class ${i}:`, {
        text: option.dataset.text,
        value: option.dataset.value,
        hasClick: !!option.onclick
    });
});
```

### **Manual test selection:**
```javascript
// Test member selection
const memberOption = document.querySelector('.member-option');
if (memberOption) {
    memberOption.click();
    console.log('Member field value:', document.getElementById('memberSearch').value);
    console.log('Member ID value:', document.getElementById('selectedMemberId').value);
}

// Test class selection
const classOption = document.querySelector('.class-option');
if (classOption) {
    classOption.click();
    console.log('Class field value:', document.getElementById('classSearch').value);
    console.log('Class ID value:', document.getElementById('selectedClassId').value);
}
```

---

## 🎯 **Expected Results:**

### **Visual Behavior:**
- ✅ Click input → Dropdown opens smoothly
- ✅ Type to search → Options filter in real-time
- ✅ Click option → Input shows selected text immediately
- ✅ Dropdown closes after selection
- ✅ Click outside → Dropdown closes

### **Console Logs:**
- ✅ `Member clicked: Phạm Thúy Hằng (👤 Thành viên) 1`
- ✅ `Class clicked: Yoga buổi sáng - 08:00-09:00 2`
- ✅ Hoặc fallback logs nếu primary handlers fail

### **Form Values:**
- ✅ `memberSearch.value` = "Phạm Thúy Hằng (👤 Thành viên)"
- ✅ `selectedMemberId.value` = "1"
- ✅ `classSearch.value` = "Yoga buổi sáng - 08:00-09:00"
- ✅ `selectedClassId.value` = "2"

### **Class Info Loading:**
- ✅ Khi chọn class → API call tới `/LopHoc/GetActiveClasses`
- ✅ Class info hiển thị ngay với grid layout
- ✅ Thông tin đầy đủ: ID, thời gian, HLV, sức chứa, giá

---

## 🚨 **Troubleshooting:**

### **Issue: Vẫn không click được**
**Solution:**
```javascript
// Force fix trong Console
document.querySelectorAll('.member-option, .class-option').forEach(option => {
    option.style.pointerEvents = 'auto';
    option.style.cursor = 'pointer';
});
```

### **Issue: Console không có logs**
**Check:**
1. JavaScript errors trong Console
2. Event listeners có được attach không
3. DOM elements có tồn tại không

### **Issue: Text không hiển thị đúng**
**Check:**
1. `data-text` attributes trong HTML
2. `dataset.text` values trong Console
3. Input field references

### **Issue: Dropdown không đóng**
**Check:**
1. Click outside logic
2. CSS z-index conflicts
3. Event propagation issues

---

## 💡 **Technical Details:**

### **Primary Event Handlers:**
```javascript
memberOptions.forEach(option => {
    option.addEventListener('click', () => {
        memberSearch.value = option.dataset.text;
        selectedMemberId.value = option.dataset.value;
        memberDropdown.classList.add('hidden');
    });
});
```

### **Fallback Handlers:**
```javascript
option.onclick = function(e) {
    e.preventDefault();
    e.stopPropagation();
    document.getElementById('memberSearch').value = this.dataset.text || this.textContent.trim();
    document.getElementById('selectedMemberId').value = this.dataset.value;
    document.getElementById('memberDropdown').classList.add('hidden');
};
```

### **Search Logic:**
```javascript
const text = (option.dataset.text || option.textContent).toLowerCase();
if (text.includes(searchTerm)) {
    option.style.display = 'block';
} else {
    option.style.display = 'none';
}
```

**Với các fixes này, dropdown selection sẽ hoạt động đúng!**
