using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Models;

namespace UserManagementApp.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public UserController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        private async Task<bool> IsCurrentUserBlocked()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.IsBlocked ?? true;
        }

        private async Task<IActionResult> HandleBlockedUser()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
            .OrderByDescending(u => u.LastLoginTime)
            .ToListAsync();
            return View(users);
        }

        private async Task<bool> ShouldRedirectToLogin()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.IsBlocked)
            {
                await _signInManager.SignOutAsync();
                return true;
            }
            return false;
        }

        [HttpPost]
        public async Task<IActionResult> BlockUsers(string[] userIds)
        {
            if (await ShouldRedirectToLogin())
            {
                return Json(new { redirectUrl = "/Identity/Account/Login" });
            }
            if (await IsCurrentUserBlocked())
                return await GetLoginRedirectResponse();

            var currentUser = await GetCurrentUser();
            if (currentUser == null)
                return await GetLoginRedirectResponse();

            bool blockingSelf = IsBlockingSelf(userIds, currentUser.Id);
            await BlockSelectedUsers(userIds);

            if (blockingSelf)
                return await HandleSelfBlocking();

            return GetSuccessBlockResponse();
        }

        private async Task<User?> GetCurrentUser() => await _userManager.GetUserAsync(User);


        private bool IsBlockingSelf(string[] userIds, string currentUserId) => 
            userIds.Contains(currentUserId);

        private async Task BlockSelectedUsers(string[] userIds)
        {
            foreach (var userId in userIds)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.IsBlocked = true;
                    await _userManager.UpdateAsync(user);
                }
            }
        }

        private async Task<JsonResult> HandleSelfBlocking()
        {
            await _signInManager.SignOutAsync();
            return Json(new { 
                redirectUrl = "/Identity/Account/Login",
                message = "You have blocked yourself and have been automatically logged out."
            });
        }

        private JsonResult GetSuccessBlockResponse() => Json(new { 
            success = true,
            message = "Users have been successfully blocked."
        });

        [HttpPost]
        public async Task<IActionResult> UnblockUsers(string[] userIds)
        {
            if (await ShouldRedirectToLogin())
            {
                return Json(new { redirectUrl = "/Identity/Account/Login" });
            }
            if (await IsCurrentUserBlocked())
                return await GetLoginRedirectResponse();

            await UnblockSelectedUsers(userIds);
            return Json(new { success = true });
        }

        private async Task UnblockSelectedUsers(string[] userIds)
        {
            foreach (var userId in userIds)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.IsBlocked = false;
                    await _userManager.UpdateAsync(user);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUsers(string[] userIds)
        {
            if (await ShouldRedirectToLogin())
            {
                return Json(new { redirectUrl = "/Identity/Account/Login" });
            }
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return await GetLoginRedirectResponse();

            if (IsDeletingSelf(userIds, currentUserId))
                return await HandleSelfDeletion(currentUserId);

            await DeleteSelectedUsers(userIds);
            return Json(new { success = true });
        }

        private string? GetCurrentUserId() => _userManager.GetUserId(User);


        private bool IsDeletingSelf(string[] userIds, string currentUserId) => 
            userIds.Contains(currentUserId);

        private async Task<JsonResult> HandleSelfDeletion(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
                await _userManager.DeleteAsync(user);

            await _signInManager.SignOutAsync();
            return Json(new { redirectUrl = "/Identity/Account/Login" });
        }

        private async Task DeleteSelectedUsers(string[] userIds)
        {
            foreach (var userId in userIds)
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                        await _userManager.DeleteAsync(user);
                }
            }
        }

        private async Task<JsonResult> GetLoginRedirectResponse()
        {
            await _signInManager.SignOutAsync();
            return Json(new { redirectUrl = "/Identity/Account/Login" });
        }
    }
}