using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using GymManagement.Web.Models.DTOs;
using System.Text;
using System.Text.RegularExpressions;

namespace GymManagement.Web.Services
{
    public class TinTucService : ITinTucService
    {
        private readonly ITinTucRepository _tinTucRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<TinTucService> _logger;

        public TinTucService(
            ITinTucRepository tinTucRepository,
            IWebHostEnvironment webHostEnvironment,
            ILogger<TinTucService> logger)
        {
            _tinTucRepository = tinTucRepository;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<IEnumerable<TinTucListDto>> GetAllAsync()
        {
            var tinTucs = await _tinTucRepository.GetAllAsync();
            return tinTucs.Select(MapToListDto);
        }

        public async Task<IEnumerable<TinTucPublicDto>> GetPublishedAsync()
        {
            var tinTucs = await _tinTucRepository.GetPublishedAsync();
            return tinTucs.Select(MapToPublicDto);
        }

        public async Task<TinTucDetailDto?> GetByIdAsync(int id)
        {
            var tinTuc = await _tinTucRepository.GetByIdAsync(id);
            return tinTuc != null ? MapToDetailDto(tinTuc) : null;
        }

        public async Task<TinTucPublicDto?> GetBySlugAsync(string slug)
        {
            var tinTuc = await _tinTucRepository.GetBySlugAsync(slug);
            return tinTuc != null ? MapToPublicDto(tinTuc) : null;
        }

        public async Task<TinTucPublicDto?> GetPublishedBySlugAsync(string slug)
        {
            var tinTuc = await _tinTucRepository.GetPublishedBySlugAsync(slug);
            return tinTuc != null ? MapToPublicDto(tinTuc) : null;
        }

        public async Task<TinTuc> CreateAsync(CreateTinTucDto dto, int tacGiaId, string tacGiaTen)
        {
            var tinTuc = new TinTuc
            {
                TieuDe = dto.TieuDe,
                MoTaNgan = dto.MoTaNgan,
                NoiDung = dto.NoiDung,
                TacGiaId = tacGiaId,
                TenTacGia = tacGiaTen,
                TrangThai = dto.TrangThai,
                NoiBat = dto.NoiBat,
                NgayXuatBan = dto.NgayXuatBan,
                MetaTitle = dto.MetaTitle ?? dto.TieuDe,
                MetaDescription = dto.MetaDescription ?? dto.MoTaNgan,
                MetaKeywords = dto.MetaKeywords
            };

            // Generate slug
            tinTuc.Slug = await GenerateSlugAsync(dto.TieuDe);

            // Save image if provided
            if (dto.AnhDaiDien != null)
            {
                tinTuc.AnhDaiDien = await SaveImageAsync(dto.AnhDaiDien);
            }

            return await _tinTucRepository.AddAsync(tinTuc);
        }

        public async Task<TinTuc> UpdateAsync(int id, EditTinTucDto dto, int nguoiCapNhatId)
        {
            var tinTuc = await _tinTucRepository.GetByIdAsync(id);
            if (tinTuc == null)
                throw new ArgumentException($"Không tìm thấy tin tức với ID {id}");

            tinTuc.TieuDe = dto.TieuDe;
            tinTuc.MoTaNgan = dto.MoTaNgan;
            tinTuc.NoiDung = dto.NoiDung;
            tinTuc.TrangThai = dto.TrangThai;
            tinTuc.NoiBat = dto.NoiBat;
            tinTuc.NgayXuatBan = dto.NgayXuatBan;
            tinTuc.NgayCapNhat = DateTime.Now;
            tinTuc.MetaTitle = dto.MetaTitle ?? dto.TieuDe;
            tinTuc.MetaDescription = dto.MetaDescription ?? dto.MoTaNgan;
            tinTuc.MetaKeywords = dto.MetaKeywords;

            // Update slug if title changed
            var newSlug = await GenerateSlugAsync(dto.TieuDe, id);
            if (tinTuc.Slug != newSlug)
            {
                tinTuc.Slug = newSlug;
            }

            // Handle image update
            if (dto.RemoveImage && !string.IsNullOrEmpty(tinTuc.AnhDaiDien))
            {
                DeleteImage(tinTuc.AnhDaiDien);
                tinTuc.AnhDaiDien = null;
            }
            else if (dto.AnhDaiDien != null)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(tinTuc.AnhDaiDien))
                {
                    DeleteImage(tinTuc.AnhDaiDien);
                }
                tinTuc.AnhDaiDien = await SaveImageAsync(dto.AnhDaiDien);
            }

            await _tinTucRepository.UpdateAsync(tinTuc);
            return tinTuc;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var tinTuc = await _tinTucRepository.GetByIdAsync(id);
            if (tinTuc == null) return false;

            // Delete image if exists
            if (!string.IsNullOrEmpty(tinTuc.AnhDaiDien))
            {
                DeleteImage(tinTuc.AnhDaiDien);
            }

            await _tinTucRepository.DeleteAsync(tinTuc);
            return true;
        }

        public async Task<IEnumerable<TinTucPublicDto>> GetNoiBatAsync(int count = 5)
        {
            var tinTucs = await _tinTucRepository.GetNoiBatAsync(count);
            return tinTucs.Select(MapToPublicDto);
        }

        public async Task<IEnumerable<TinTucPublicDto>> SearchAsync(string keyword)
        {
            var tinTucs = await _tinTucRepository.SearchAsync(keyword);
            return tinTucs.Select(MapToPublicDto);
        }

        public async Task<IEnumerable<TinTucPublicDto>> GetByTacGiaAsync(int tacGiaId)
        {
            var tinTucs = await _tinTucRepository.GetByTacGiaIdAsync(tacGiaId);
            return tinTucs.Where(t => t.TrangThai == "PUBLISHED").Select(MapToPublicDto);
        }

        public async Task<string> GenerateSlugAsync(string title, int? excludeId = null)
        {
            var slug = ConvertToSlug(title);
            var originalSlug = slug;
            var counter = 1;

            while (await _tinTucRepository.SlugExistsAsync(slug, excludeId))
            {
                slug = $"{originalSlug}-{counter}";
                counter++;
            }

            return slug;
        }

        public async Task<string?> SaveImageAsync(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return null;

            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tintuc");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                return $"/uploads/tintuc/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving image");
                return null;
            }
        }

        public void DeleteImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return;

            try
            {
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image: {ImagePath}", imagePath);
            }
        }

        public async Task<int> IncrementViewAsync(int id)
        {
            return await _tinTucRepository.IncrementLuotXemAsync(id);
        }

        public async Task<IEnumerable<TinTucPublicDto>> GetRelatedAsync(int id, int count = 4)
        {
            var tinTucs = await _tinTucRepository.GetRelatedAsync(id, count);
            return tinTucs.Select(MapToPublicDto);
        }

        private string ConvertToSlug(string text)
        {
            // Convert to lowercase
            text = text.ToLowerInvariant();

            // Remove Vietnamese accents
            text = RemoveVietnameseAccents(text);

            // Replace spaces with hyphens
            text = Regex.Replace(text, @"\s+", "-");

            // Remove invalid characters
            text = Regex.Replace(text, @"[^a-z0-9\-]", "");

            // Remove multiple hyphens
            text = Regex.Replace(text, @"\-+", "-");

            // Trim hyphens from start and end
            text = text.Trim('-');

            return text;
        }

        private string RemoveVietnameseAccents(string text)
        {
            var sb = new StringBuilder(text);
            sb.Replace("à", "a").Replace("á", "a").Replace("ạ", "a").Replace("ả", "a").Replace("ã", "a");
            sb.Replace("ầ", "a").Replace("ấ", "a").Replace("ậ", "a").Replace("ẩ", "a").Replace("ẫ", "a");
            sb.Replace("ằ", "a").Replace("ắ", "a").Replace("ặ", "a").Replace("ẳ", "a").Replace("ẵ", "a");
            sb.Replace("è", "e").Replace("é", "e").Replace("ẹ", "e").Replace("ẻ", "e").Replace("ẽ", "e");
            sb.Replace("ề", "e").Replace("ế", "e").Replace("ệ", "e").Replace("ể", "e").Replace("ễ", "e");
            sb.Replace("ì", "i").Replace("í", "i").Replace("ị", "i").Replace("ỉ", "i").Replace("ĩ", "i");
            sb.Replace("ò", "o").Replace("ó", "o").Replace("ọ", "o").Replace("ỏ", "o").Replace("õ", "o");
            sb.Replace("ồ", "o").Replace("ố", "o").Replace("ộ", "o").Replace("ổ", "o").Replace("ỗ", "o");
            sb.Replace("ờ", "o").Replace("ớ", "o").Replace("ợ", "o").Replace("ở", "o").Replace("ỡ", "o");
            sb.Replace("ù", "u").Replace("ú", "u").Replace("ụ", "u").Replace("ủ", "u").Replace("ũ", "u");
            sb.Replace("ừ", "u").Replace("ứ", "u").Replace("ự", "u").Replace("ử", "u").Replace("ữ", "u");
            sb.Replace("ỳ", "y").Replace("ý", "y").Replace("ỵ", "y").Replace("ỷ", "y").Replace("ỹ", "y");
            sb.Replace("đ", "d");
            return sb.ToString();
        }

        private TinTucListDto MapToListDto(TinTuc tinTuc)
        {
            return new TinTucListDto
            {
                TinTucId = tinTuc.TinTucId,
                TieuDe = tinTuc.TieuDe,
                MoTaNgan = tinTuc.MoTaNgan,
                AnhDaiDien = tinTuc.AnhDaiDien,
                NgayTao = tinTuc.NgayTao,
                NgayXuatBan = tinTuc.NgayXuatBan,
                TenTacGia = tinTuc.TenTacGia ?? tinTuc.TacGia?.Ho + " " + tinTuc.TacGia?.Ten,
                LuotXem = tinTuc.LuotXem,
                TrangThai = tinTuc.TrangThai,
                NoiBat = tinTuc.NoiBat,
                Slug = tinTuc.Slug
            };
        }

        private TinTucDetailDto MapToDetailDto(TinTuc tinTuc)
        {
            return new TinTucDetailDto
            {
                TinTucId = tinTuc.TinTucId,
                TieuDe = tinTuc.TieuDe,
                MoTaNgan = tinTuc.MoTaNgan,
                NoiDung = tinTuc.NoiDung,
                AnhDaiDien = tinTuc.AnhDaiDien,
                NgayTao = tinTuc.NgayTao,
                NgayCapNhat = tinTuc.NgayCapNhat,
                NgayXuatBan = tinTuc.NgayXuatBan,
                TacGiaId = tinTuc.TacGiaId,
                TenTacGia = tinTuc.TenTacGia ?? tinTuc.TacGia?.Ho + " " + tinTuc.TacGia?.Ten,
                LuotXem = tinTuc.LuotXem,
                TrangThai = tinTuc.TrangThai,
                NoiBat = tinTuc.NoiBat,
                Slug = tinTuc.Slug,
                MetaTitle = tinTuc.MetaTitle,
                MetaDescription = tinTuc.MetaDescription,
                MetaKeywords = tinTuc.MetaKeywords
            };
        }

        private TinTucPublicDto MapToPublicDto(TinTuc tinTuc)
        {
            return new TinTucPublicDto
            {
                TinTucId = tinTuc.TinTucId,
                TieuDe = tinTuc.TieuDe,
                MoTaNgan = tinTuc.MoTaNgan,
                NoiDung = tinTuc.NoiDung,
                AnhDaiDien = tinTuc.AnhDaiDien,
                NgayXuatBan = tinTuc.NgayXuatBan,
                TenTacGia = tinTuc.TenTacGia ?? tinTuc.TacGia?.Ho + " " + tinTuc.TacGia?.Ten,
                LuotXem = tinTuc.LuotXem,
                Slug = tinTuc.Slug,
                NoiBat = tinTuc.NoiBat
            };
        }
    }
}
