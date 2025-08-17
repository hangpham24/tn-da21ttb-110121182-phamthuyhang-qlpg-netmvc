using Microsoft.AspNetCore.Mvc;
using GymManagement.Web.Services;

namespace GymManagement.Web.ViewComponents
{
    public class UserAvatarViewComponent : ViewComponent
    {
        private readonly IUserSessionService _userSessionService;
        private readonly INguoiDungService _nguoiDungService;

        public UserAvatarViewComponent(
            IUserSessionService userSessionService,
            INguoiDungService nguoiDungService)
        {
            _userSessionService = userSessionService;
            _nguoiDungService = nguoiDungService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string cssClass = "w-8 h-8", bool showName = false)
        {
            var currentUser = await _userSessionService.GetCurrentUserAsync();
            
            if (currentUser == null || currentUser.NguoiDungId == null)
            {
                return View(new UserAvatarViewModel
                {
                    AnhDaiDien = null,
                    HoTen = "User",
                    CssClass = cssClass,
                    ShowName = showName
                });
            }

            var nguoiDung = await _nguoiDungService.GetByIdAsync(currentUser.NguoiDungId.Value);
            
            return View(new UserAvatarViewModel
            {
                AnhDaiDien = nguoiDung?.AnhDaiDien,
                HoTen = currentUser.HoTen ?? "User",
                CssClass = cssClass,
                ShowName = showName
            });
        }
    }

    public class UserAvatarViewModel
    {
        public string? AnhDaiDien { get; set; }
        public string HoTen { get; set; } = "User";
        public string CssClass { get; set; } = "w-8 h-8";
        public bool ShowName { get; set; }
    }
}
