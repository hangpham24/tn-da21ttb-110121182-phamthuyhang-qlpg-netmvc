@echo off
echo 🤖 Face-API.js Models Auto Downloader
echo ====================================

set MODELS_DIR=wwwroot\lib\face-api\models
set BASE_URL=https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights

echo 📦 Creating models directory...
if not exist "%MODELS_DIR%" mkdir "%MODELS_DIR%"

echo.
echo 🔄 Downloading TinyFaceDetector models...
curl -L -o "%MODELS_DIR%\tiny_face_detector_model-weights_manifest.json" "%BASE_URL%/tiny_face_detector_model-weights_manifest.json"
curl -L -o "%MODELS_DIR%\tiny_face_detector_model-shard1" "%BASE_URL%/tiny_face_detector_model-shard1"

echo.
echo 🔄 Downloading FaceLandmark68Net models...
curl -L -o "%MODELS_DIR%\face_landmark_68_model-weights_manifest.json" "%BASE_URL%/face_landmark_68_model-weights_manifest.json"
curl -L -o "%MODELS_DIR%\face_landmark_68_model-shard1" "%BASE_URL%/face_landmark_68_model-shard1"

echo.
echo 🔄 Downloading FaceRecognitionNet models...
curl -L -o "%MODELS_DIR%\face_recognition_model-weights_manifest.json" "%BASE_URL%/face_recognition_model-weights_manifest.json"
curl -L -o "%MODELS_DIR%\face_recognition_model-shard1" "%BASE_URL%/face_recognition_model-shard1"
curl -L -o "%MODELS_DIR%\face_recognition_model-shard2" "%BASE_URL%/face_recognition_model-shard2"

echo.
echo 📁 Files in models directory:
dir "%MODELS_DIR%" /B

echo.
echo 🎉 Download completed!
echo 🚀 Face Recognition system is ready!
echo.
echo 🎯 Next Steps:
echo 1. Start the application: dotnet run
echo 2. Navigate to: https://localhost:5003/FaceTest
echo 3. Click 'Start Camera' to test face detection
echo.
pause
