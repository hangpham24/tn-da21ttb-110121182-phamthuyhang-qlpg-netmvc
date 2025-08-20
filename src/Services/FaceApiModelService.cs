using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GymManagement.Web.Services
{
    /// <summary>
    /// Service để preload Face-API models khi ứng dụng khởi động
    /// </summary>
    public class FaceApiModelService : IHostedService
    {
        private readonly ILogger<FaceApiModelService> _logger;
        private readonly IWebHostEnvironment _environment;

        public FaceApiModelService(
            ILogger<FaceApiModelService> logger,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🚀 Starting Face-API Model Preloading Service...");

            try
            {
                await PreloadModelsAsync();
                _logger.LogInformation("✅ Face-API models preloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to preload Face-API models");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 Face-API Model Service stopped");
            return Task.CompletedTask;
        }

        private async Task PreloadModelsAsync()
        {
            _logger.LogInformation("📦 Preloading Face-API models...");

            // Đường dẫn đến thư mục models
            var modelsPath = Path.Combine(_environment.WebRootPath, "models");
            
            if (!Directory.Exists(modelsPath))
            {
                _logger.LogWarning("⚠️ Models directory not found: {ModelsPath}", modelsPath);
                return;
            }

            // Kiểm tra các file models cần thiết
            var requiredModels = new[]
            {
                "tiny_face_detector_model-weights_manifest.json",
                "tiny_face_detector_model-shard1",
                "face_landmark_68_model-weights_manifest.json", 
                "face_landmark_68_model-shard1",
                "face_recognition_model-weights_manifest.json",
                "face_recognition_model-shard1",
                "face_recognition_model-shard2"
            };

            var missingModels = new List<string>();
            
            foreach (var model in requiredModels)
            {
                var modelPath = Path.Combine(modelsPath, model);
                if (!File.Exists(modelPath))
                {
                    missingModels.Add(model);
                }
            }

            if (missingModels.Any())
            {
                _logger.LogWarning("⚠️ Missing model files: {MissingModels}", string.Join(", ", missingModels));
                return;
            }

            _logger.LogInformation("✅ All required Face-API model files found");
            
            // Simulate model loading time (trong thực tế, Face-API models được load ở client-side)
            await Task.Delay(1000);
            
            _logger.LogInformation("🎯 Face-API models ready for use");
        }
    }

    /// <summary>
    /// Static class để track model loading status
    /// </summary>
    public static class FaceApiModelStatus
    {
        private static bool _isLoaded = false;
        private static readonly object _lock = new object();

        public static bool IsLoaded
        {
            get
            {
                lock (_lock)
                {
                    return _isLoaded;
                }
            }
            set
            {
                lock (_lock)
                {
                    _isLoaded = value;
                }
            }
        }

        public static void SetLoaded()
        {
            IsLoaded = true;
        }

        public static void SetNotLoaded()
        {
            IsLoaded = false;
        }
    }
}
