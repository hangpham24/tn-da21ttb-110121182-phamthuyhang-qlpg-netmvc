# Debug Script cho Dropdown Selection Issue

## 🐛 **Vấn đề:**
- User search và click vào option trong dropdown
- Text không hiển thị trong input field
- Cả Member và Class dropdown đều có vấn đề này

## 🔍 **Debug Steps:**

### **Step 1: Kiểm tra Console Logs**
1. **Mở Developer Tools** (F12)
2. **Truy cập:** http://localhost:5003/Booking/Create
3. **Click vào Member dropdown** và chọn một option
4. **Check Console:** Phải thấy log `Member clicked: [name] [id]`
5. **Click vào Class dropdown** và chọn một option  
6. **Check Console:** Phải thấy log `Class clicked: [name] [id]`

### **Step 2: Kiểm tra DOM Elements**
**Mở Console và chạy:**
```javascript
// Check if elements exist
console.log('memberSearch:', document.getElementById('memberSearch'));
console.log('memberDropdown:', document.getElementById('memberDropdown'));
console.log('selectedMemberId:', document.getElementById('selectedMemberId'));
console.log('classSearch:', document.getElementById('classSearch'));
console.log('classDropdown:', document.getElementById('classDropdown'));
console.log('selectedClassId:', document.getElementById('selectedClassId'));

// Check member options
console.log('member options:', document.querySelectorAll('.member-option'));
console.log('class options:', document.querySelectorAll('.class-option'));
```

### **Step 3: Kiểm tra Event Listeners**
**Chạy trong Console:**
```javascript
// Test member dropdown manually
const memberOptions = document.querySelectorAll('.member-option');
memberOptions.forEach((option, index) => {
    console.log(`Member option ${index}:`, {
        text: option.dataset.text,
        value: option.dataset.value,
        textContent: option.textContent.trim()
    });
});

// Test class dropdown manually
const classOptions = document.querySelectorAll('.class-option');
classOptions.forEach((option, index) => {
    console.log(`Class option ${index}:`, {
        text: option.dataset.text,
        value: option.dataset.value,
        textContent: option.textContent.trim()
    });
});
```

### **Step 4: Manual Click Test**
**Chạy trong Console:**
```javascript
// Test member selection manually
const memberSearch = document.getElementById('memberSearch');
const selectedMemberId = document.getElementById('selectedMemberId');
const firstMemberOption = document.querySelector('.member-option');

if (firstMemberOption) {
    console.log('Testing member selection...');
    memberSearch.value = firstMemberOption.dataset.text;
    selectedMemberId.value = firstMemberOption.dataset.value;
    console.log('Member search value:', memberSearch.value);
    console.log('Selected member ID:', selectedMemberId.value);
}

// Test class selection manually
const classSearch = document.getElementById('classSearch');
const selectedClassId = document.getElementById('selectedClassId');
const firstClassOption = document.querySelector('.class-option');

if (firstClassOption) {
    console.log('Testing class selection...');
    classSearch.value = firstClassOption.dataset.text;
    selectedClassId.value = firstClassOption.dataset.value;
    console.log('Class search value:', classSearch.value);
    console.log('Selected class ID:', selectedClassId.value);
}
```

## 🔧 **Possible Issues & Solutions:**

### **Issue 1: Event Listeners không được attach**
**Symptom:** Không có console logs khi click
**Solution:** Check if `initSearchableDropdowns()` được gọi

**Test:**
```javascript
// Check if function exists
console.log('initSearchableDropdowns:', typeof initSearchableDropdowns);

// Re-run initialization
if (typeof initSearchableDropdowns === 'function') {
    initSearchableDropdowns();
    console.log('Re-initialized dropdowns');
}
```

### **Issue 2: Dataset attributes bị thiếu**
**Symptom:** `option.dataset.text` hoặc `option.dataset.value` là undefined
**Solution:** Check HTML attributes

**Test:**
```javascript
document.querySelectorAll('.member-option').forEach(option => {
    console.log('Member option HTML:', option.outerHTML);
});

document.querySelectorAll('.class-option').forEach(option => {
    console.log('Class option HTML:', option.outerHTML);
});
```

### **Issue 3: Click event bị prevent**
**Symptom:** Event fires nhưng value không update
**Solution:** Check for event.preventDefault() hoặc event.stopPropagation()

**Test:**
```javascript
// Add temporary click handler
document.querySelectorAll('.member-option').forEach(option => {
    option.addEventListener('click', function(e) {
        console.log('Direct click handler:', this.dataset.text);
        e.stopPropagation();
        document.getElementById('memberSearch').value = this.dataset.text;
        document.getElementById('selectedMemberId').value = this.dataset.value;
    });
});
```

### **Issue 4: CSS z-index hoặc pointer-events**
**Symptom:** Click không register
**Solution:** Check CSS styles

**Test:**
```javascript
// Check computed styles
const memberDropdown = document.getElementById('memberDropdown');
const computedStyle = window.getComputedStyle(memberDropdown);
console.log('Member dropdown styles:', {
    display: computedStyle.display,
    visibility: computedStyle.visibility,
    pointerEvents: computedStyle.pointerEvents,
    zIndex: computedStyle.zIndex
});
```

## 🎯 **Expected Behavior:**

### **Member Dropdown:**
1. Click input → Dropdown opens
2. Type to search → Options filter
3. Click option → Input shows selected text, hidden field gets ID
4. Click outside → Dropdown closes

### **Class Dropdown:**
1. Click input → Dropdown opens
2. Type to search → Options filter  
3. Click option → Input shows selected text, hidden field gets ID, class info loads
4. Click outside → Dropdown closes

## 🚨 **Quick Fix Test:**

**Nếu tất cả fails, thử quick fix này trong Console:**
```javascript
// Force fix member dropdown
document.querySelectorAll('.member-option').forEach(option => {
    option.onclick = function() {
        console.log('Force click:', this.dataset.text);
        document.getElementById('memberSearch').value = this.dataset.text;
        document.getElementById('selectedMemberId').value = this.dataset.value;
        document.getElementById('memberDropdown').classList.add('hidden');
    };
});

// Force fix class dropdown
document.querySelectorAll('.class-option').forEach(option => {
    option.onclick = function() {
        console.log('Force click:', this.dataset.text);
        document.getElementById('classSearch').value = this.dataset.text;
        document.getElementById('selectedClassId').value = this.dataset.value;
        document.getElementById('classDropdown').classList.add('hidden');
        loadClassInfo(this.dataset.value);
    };
});
```

## 📋 **Debug Checklist:**

- [ ] Console logs xuất hiện khi click options
- [ ] DOM elements tồn tại (không null)
- [ ] Dataset attributes có values
- [ ] Event listeners được attach
- [ ] CSS không block pointer events
- [ ] No JavaScript errors trong console
- [ ] Hidden fields được update
- [ ] Input fields hiển thị text

## 💡 **Common Solutions:**

1. **Re-run initialization:** `initSearchableDropdowns()`
2. **Check HTML structure:** Ensure data-text và data-value attributes
3. **CSS conflicts:** Check z-index và pointer-events
4. **Event delegation:** Use event.target properly
5. **Timing issues:** Ensure DOM is loaded before attaching events

**Chạy debug script này để tìm root cause của vấn đề!**
