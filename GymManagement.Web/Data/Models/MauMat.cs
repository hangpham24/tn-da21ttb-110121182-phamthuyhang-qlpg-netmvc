using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Data.Models
{
    public class MauMat
    {
        public int MauMatId { get; set; }
        
        [Required]
        public int NguoiDungId { get; set; }
        
        [Required]
        public byte[] Embedding { get; set; } = null!;
        
        public DateTime NgayTao { get; set; }
        
        [StringLength(50)]
        public string ThuatToan { get; set; } = "Face-API.js";

        // Navigation properties
        public virtual NguoiDung NguoiDung { get; set; } = null!;
    }
}
