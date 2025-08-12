# Face-API.js Models Setup

## 📦 Required Models

Face Recognition system cần 3 loại models:

### 1. TinyFaceDetector
- `tiny_face_detector_model-weights_manifest.json`
- `tiny_face_detector_model-shard1`

### 2. FaceLandmark68Net  
- `face_landmark_68_model-weights_manifest.json`
- `face_landmark_68_model-shard1`

### 3. FaceRecognitionNet
- `face_recognition_model-weights_manifest.json`
- `face_recognition_model-shard1`
- `face_recognition_model-shard2`

## 🚀 Auto Download

Chạy một trong các scripts sau:

### PowerShell:
```powershell
.\download-models.ps1
```

### Batch:
```batch
download-models.bat
```

### Manual Download:
```bash
# Tạo thư mục
mkdir -p wwwroot/lib/face-api/models

# Base URL
BASE_URL="https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights"

# Download TinyFaceDetector
curl -o wwwroot/lib/face-api/models/tiny_face_detector_model-weights_manifest.json $BASE_URL/tiny_face_detector_model-weights_manifest.json
curl -o wwwroot/lib/face-api/models/tiny_face_detector_model-shard1 $BASE_URL/tiny_face_detector_model-shard1

# Download FaceLandmark68Net
curl -o wwwroot/lib/face-api/models/face_landmark_68_model-weights_manifest.json $BASE_URL/face_landmark_68_model-weights_manifest.json
curl -o wwwroot/lib/face-api/models/face_landmark_68_model-shard1 $BASE_URL/face_landmark_68_model-shard1

# Download FaceRecognitionNet
curl -o wwwroot/lib/face-api/models/face_recognition_model-weights_manifest.json $BASE_URL/face_recognition_model-weights_manifest.json
curl -o wwwroot/lib/face-api/models/face_recognition_model-shard1 $BASE_URL/face_recognition_model-shard1
curl -o wwwroot/lib/face-api/models/face_recognition_model-shard2 $BASE_URL/face_recognition_model-shard2
```

## ✅ Verification

Kiểm tra tất cả files đã có:
```bash
ls -la wwwroot/lib/face-api/models/
```

Phải có 8 files (bao gồm README.md).

## 🎯 Testing

1. Start server: `dotnet run`
2. Navigate to: `https://localhost:5003/FaceTest`
3. Check System Status: Should show `✅ Ready`
4. Test camera and face detection

## 🔧 Troubleshooting

- **Models not loading**: Check internet connection and file permissions
- **Face detection error**: Ensure all 3 model types are present
- **Camera not working**: Check browser permissions and HTTPS
