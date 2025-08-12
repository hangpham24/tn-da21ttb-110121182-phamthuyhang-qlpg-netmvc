using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace GymManagement.Web.Services
{
    public class FaceRecognitionService : IFaceRecognitionService
    {
        private readonly IMauMatRepository _mauMatRepository;
        private readonly INguoiDungRepository _nguoiDungRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FaceRecognitionService> _logger;
        private readonly IConfiguration _configuration;
        
        private const double DEFAULT_THRESHOLD = 0.6;
        private const string CACHE_KEY_PREFIX = "face_recognition_";

        public FaceRecognitionService(
            IMauMatRepository mauMatRepository,
            INguoiDungRepository nguoiDungRepository,
            IMemoryCache cache,
            ILogger<FaceRecognitionService> logger,
            IConfiguration configuration)
        {
            _mauMatRepository = mauMatRepository;
            _nguoiDungRepository = nguoiDungRepository;
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<bool> RegisterFaceAsync(int nguoiDungId, float[] faceDescriptor)
        {
            try
            {
                // Validate input
                if (faceDescriptor == null || faceDescriptor.Length != 128)
                {
                    _logger.LogWarning("Invalid face descriptor length: {Length}", faceDescriptor?.Length);
                    return false;
                }

                // Check if face already exists
                if (await IsFaceAlreadyRegisteredAsync(faceDescriptor, nguoiDungId))
                {
                    _logger.LogWarning("Face already registered for user {UserId}", nguoiDungId);
                    return false;
                }

                // Create MauMat record
                var mauMat = new MauMat
                {
                    NguoiDungId = nguoiDungId,
                    Embedding = ConvertDescriptorToBytes(faceDescriptor),
                    NgayTao = DateTime.Now,
                    ThuatToan = "face-api.js"
                };

                await _mauMatRepository.AddAsync(mauMat);
                
                // Clear cache
                _cache.Remove($"{CACHE_KEY_PREFIX}user_{nguoiDungId}");
                
                _logger.LogInformation("Face registered successfully for user {UserId}", nguoiDungId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering face for user {UserId}", nguoiDungId);
                return false;
            }
        }

        public async Task<bool> RegisterMultipleFacesAsync(int nguoiDungId, List<float[]> faceDescriptors)
        {
            try
            {
                var successCount = 0;
                foreach (var descriptor in faceDescriptors)
                {
                    if (await RegisterFaceAsync(nguoiDungId, descriptor))
                    {
                        successCount++;
                    }
                }
                
                return successCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering multiple faces for user {UserId}", nguoiDungId);
                return false;
            }
        }

        public async Task<int?> IdentifyMemberAsync(float[] faceDescriptor, double threshold = DEFAULT_THRESHOLD)
        {
            try
            {
                var result = await RecognizeFaceAsync(faceDescriptor);
                return result.Success && result.Confidence >= threshold ? result.MemberId : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error identifying member");
                return null;
            }
        }

        public async Task<FaceRecognitionResult> RecognizeFaceAsync(float[] faceDescriptor)
        {
            try
            {
                // Get all registered faces
                var allFaces = await _mauMatRepository.GetAllWithUsersAsync();
                
                double bestSimilarity = 0;
                MauMat? bestMatch = null;

                // Compare with all registered faces
                foreach (var face in allFaces)
                {
                    var storedDescriptor = ConvertBytesToDescriptor(face.Embedding);
                    var similarity = await CalculateSimilarityAsync(faceDescriptor, storedDescriptor);
                    
                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        bestMatch = face;
                    }
                }

                // Log recognition attempt
                await LogRecognitionAttemptAsync(bestMatch?.NguoiDungId, bestSimilarity >= DEFAULT_THRESHOLD, bestSimilarity);

                if (bestMatch != null && bestSimilarity >= DEFAULT_THRESHOLD)
                {
                    return new FaceRecognitionResult
                    {
                        Success = true,
                        MemberId = bestMatch.NguoiDungId,
                        MemberName = $"{bestMatch.NguoiDung?.Ho} {bestMatch.NguoiDung?.Ten}",
                        Confidence = bestSimilarity,
                        Message = "Nhận diện thành công"
                    };
                }

                return new FaceRecognitionResult
                {
                    Success = false,
                    Confidence = bestSimilarity,
                    Message = "Không nhận diện được khuôn mặt"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during face recognition");
                return new FaceRecognitionResult
                {
                    Success = false,
                    Message = "Lỗi hệ thống khi nhận diện"
                };
            }
        }

        public async Task<double> CalculateSimilarityAsync(float[] descriptor1, float[] descriptor2)
        {
            if (descriptor1.Length != descriptor2.Length)
                return 0;

            // Calculate Euclidean distance
            double sum = 0;
            for (int i = 0; i < descriptor1.Length; i++)
            {
                double diff = descriptor1[i] - descriptor2[i];
                sum += diff * diff;
            }
            
            double distance = Math.Sqrt(sum);
            
            // Convert distance to similarity (0-1 scale)
            // Lower distance = higher similarity
            double similarity = Math.Max(0, 1 - (distance / 2.0));
            
            return similarity;
        }

        public byte[] ConvertDescriptorToBytes(float[] descriptor)
        {
            var bytes = new byte[descriptor.Length * 4];
            Buffer.BlockCopy(descriptor, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public float[] ConvertBytesToDescriptor(byte[] bytes)
        {
            var descriptor = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, descriptor, 0, bytes.Length);
            return descriptor;
        }

        private async Task LogRecognitionAttemptAsync(int? memberId, bool success, double confidence)
        {
            try
            {
                // This would typically log to a separate table
                _logger.LogInformation("Face recognition attempt - Member: {MemberId}, Success: {Success}, Confidence: {Confidence}", 
                    memberId, success, confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging recognition attempt");
            }
        }

        // Placeholder implementations for remaining interface methods
        public async Task<IEnumerable<MauMat>> GetMemberFacesAsync(int nguoiDungId)
        {
            return await _mauMatRepository.GetByNguoiDungIdAsync(nguoiDungId);
        }

        public async Task<IEnumerable<MauMat>> GetAllFacesAsync()
        {
            return await _mauMatRepository.GetAllWithUsersAsync();
        }

        public async Task<bool> UpdateFaceAsync(int mauMatId, float[] newDescriptor)
        {
            try
            {
                var mauMat = await _mauMatRepository.GetByIdAsync(mauMatId);
                if (mauMat == null) return false;

                mauMat.Embedding = ConvertDescriptorToBytes(newDescriptor);
                await _mauMatRepository.UpdateAsync(mauMat);
                
                _cache.Remove($"{CACHE_KEY_PREFIX}user_{mauMat.NguoiDungId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating face {FaceId}", mauMatId);
                return false;
            }
        }

        public async Task<bool> DeleteFaceAsync(int mauMatId)
        {
            try
            {
                var mauMat = await _mauMatRepository.GetByIdAsync(mauMatId);
                if (mauMat == null) return false;

                await _mauMatRepository.DeleteAsync(mauMatId);
                _cache.Remove($"{CACHE_KEY_PREFIX}user_{mauMat.NguoiDungId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting face {FaceId}", mauMatId);
                return false;
            }
        }

        public async Task<bool> DeleteAllMemberFacesAsync(int nguoiDungId)
        {
            try
            {
                var faces = await _mauMatRepository.GetByNguoiDungIdAsync(nguoiDungId);
                foreach (var face in faces)
                {
                    await _mauMatRepository.DeleteAsync(face.MauMatId);
                }
                
                _cache.Remove($"{CACHE_KEY_PREFIX}user_{nguoiDungId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all faces for user {UserId}", nguoiDungId);
                return false;
            }
        }

        public async Task<bool> IsFaceAlreadyRegisteredAsync(float[] faceDescriptor, int? excludeNguoiDungId = null)
        {
            try
            {
                var allFaces = await _mauMatRepository.GetAllAsync();
                
                foreach (var face in allFaces)
                {
                    if (excludeNguoiDungId.HasValue && face.NguoiDungId == excludeNguoiDungId.Value)
                        continue;

                    var storedDescriptor = ConvertBytesToDescriptor(face.Embedding);
                    var similarity = await CalculateSimilarityAsync(faceDescriptor, storedDescriptor);
                    
                    if (similarity > 0.8) // High threshold for duplicate detection
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for duplicate face");
                return false;
            }
        }

        public async Task<bool> ValidateFaceQualityAsync(float[] faceDescriptor)
        {
            // Basic validation - check if descriptor has reasonable values
            if (faceDescriptor == null || faceDescriptor.Length != 128)
                return false;

            // Check for all zeros or extreme values
            var hasValidValues = faceDescriptor.Any(x => Math.Abs(x) > 0.001);
            var hasReasonableRange = faceDescriptor.All(x => Math.Abs(x) < 10.0);
            
            return hasValidValues && hasReasonableRange;
        }

        // Placeholder implementations for remaining methods
        public async Task<FaceRecognitionStats> GetRecognitionStatsAsync()
        {
            var totalFaces = await _mauMatRepository.CountAsync();
            var allMembers = await _nguoiDungRepository.GetAllAsync();
            var totalMembers = allMembers.Count();

            return new FaceRecognitionStats
            {
                TotalRegisteredFaces = totalFaces,
                TotalMembers = totalMembers,
                TodayRecognitions = 0, // Would need separate logging table
                SuccessfulRecognitions = 0,
                FailedRecognitions = 0,
                SuccessRate = 0,
                LastRecognition = DateTime.Now
            };
        }

        public async Task<IEnumerable<FaceRecognitionLog>> GetRecognitionLogsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            // Would need separate logging table implementation
            return new List<FaceRecognitionLog>();
        }

        public async Task<bool> BackupFaceDataAsync(string filePath)
        {
            try
            {
                var allFaces = await _mauMatRepository.GetAllWithUsersAsync();
                var json = JsonSerializer.Serialize(allFaces, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backing up face data");
                return false;
            }
        }

        public async Task<bool> RestoreFaceDataAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;
                
                var json = await File.ReadAllTextAsync(filePath);
                var faces = JsonSerializer.Deserialize<List<MauMat>>(json);
                
                if (faces != null)
                {
                    foreach (var face in faces)
                    {
                        await _mauMatRepository.AddAsync(face);
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring face data");
                return false;
            }
        }
    }
}
