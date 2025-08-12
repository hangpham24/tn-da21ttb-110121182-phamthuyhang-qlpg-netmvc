using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Services
{
    public interface IFaceRecognitionService
    {
        // Face Registration
        Task<bool> RegisterFaceAsync(int nguoiDungId, float[] faceDescriptor);
        Task<bool> RegisterMultipleFacesAsync(int nguoiDungId, List<float[]> faceDescriptors);
        
        // Face Recognition
        Task<int?> IdentifyMemberAsync(float[] faceDescriptor, double threshold = 0.6);
        Task<FaceRecognitionResult> RecognizeFaceAsync(float[] faceDescriptor);
        
        // Face Management (CRUD)
        Task<IEnumerable<MauMat>> GetMemberFacesAsync(int nguoiDungId);
        Task<IEnumerable<MauMat>> GetAllFacesAsync();
        Task<bool> UpdateFaceAsync(int mauMatId, float[] newDescriptor);
        Task<bool> DeleteFaceAsync(int mauMatId);
        Task<bool> DeleteAllMemberFacesAsync(int nguoiDungId);
        
        // Face Validation
        Task<bool> IsFaceAlreadyRegisteredAsync(float[] faceDescriptor, int? excludeNguoiDungId = null);
        Task<double> CalculateSimilarityAsync(float[] descriptor1, float[] descriptor2);
        Task<bool> ValidateFaceQualityAsync(float[] faceDescriptor);
        
        // Statistics and Analytics
        Task<FaceRecognitionStats> GetRecognitionStatsAsync();
        Task<IEnumerable<FaceRecognitionLog>> GetRecognitionLogsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        
        // Utility Methods
        byte[] ConvertDescriptorToBytes(float[] descriptor);
        float[] ConvertBytesToDescriptor(byte[] bytes);
        Task<bool> BackupFaceDataAsync(string filePath);
        Task<bool> RestoreFaceDataAsync(string filePath);
    }

    // DTOs and Result Classes
    public class FaceRecognitionResult
    {
        public bool Success { get; set; }
        public int? MemberId { get; set; }
        public string? MemberName { get; set; }
        public double Confidence { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime RecognitionTime { get; set; } = DateTime.Now;
    }

    public class FaceRecognitionStats
    {
        public int TotalRegisteredFaces { get; set; }
        public int TotalMembers { get; set; }
        public int TodayRecognitions { get; set; }
        public int SuccessfulRecognitions { get; set; }
        public int FailedRecognitions { get; set; }
        public double SuccessRate { get; set; }
        public DateTime LastRecognition { get; set; }
    }

    public class FaceRecognitionLog
    {
        public int LogId { get; set; }
        public int? MemberId { get; set; }
        public string? MemberName { get; set; }
        public bool Success { get; set; }
        public double Confidence { get; set; }
        public DateTime Timestamp { get; set; }
        public string? ErrorMessage { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
