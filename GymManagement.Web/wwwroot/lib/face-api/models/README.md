# Face-API.js Models

This directory contains the pre-trained models for Face-API.js face recognition.

## Required Models

Download the following models from the Face-API.js repository and place them in this directory:

1. **tiny_face_detector_model-weights_manifest.json**
2. **tiny_face_detector_model-shard1**
3. **face_recognition_model-weights_manifest.json**
4. **face_recognition_model-shard1**
5. **face_recognition_model-shard2**

## Download Instructions

1. Go to https://github.com/justadudewhohacks/face-api.js/tree/master/weights
2. Download the required model files
3. Place them in this directory

## Model Descriptions

- **TinyFaceDetector**: Lightweight face detection model
- **FaceRecognitionNet**: Face recognition and descriptor extraction model

## Usage

The models will be automatically loaded by the Face-API.js library when the application starts.

```javascript
await faceapi.nets.tinyFaceDetector.loadFromUri('/lib/face-api/models');
await faceapi.nets.faceRecognitionNet.loadFromUri('/lib/face-api/models');
```

## File Structure

```
models/
├── tiny_face_detector_model-weights_manifest.json
├── tiny_face_detector_model-shard1
├── face_recognition_model-weights_manifest.json
├── face_recognition_model-shard1
└── face_recognition_model-shard2
```

## Notes

- Models are approximately 5-10MB total
- Models are loaded once when the application starts
- Models run entirely in the browser (client-side)
- No data is sent to external servers
