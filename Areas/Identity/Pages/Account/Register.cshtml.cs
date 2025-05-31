#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UserManagementApp.Models;

namespace UserManagementApp.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            SignInManager<User> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Username")]
            public string UserName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = await GetExternalLoginsAsync();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = await GetExternalLoginsAsync();

            if (!ModelState.IsValid)
                return Page();

            if (await IsEmailAlreadyRegistered())
            {
                ModelState.AddModelError(nameof(Input.Email), "This email address is already registered.");
                return Page();
            }

            var user = CreateUser();
            await SetUserCredentials(user);

            try
            {
                return await TryRegisterUser(user, returnUrl);
            }
            catch (Exception ex)
            {
                LogRegistrationError(ex);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred during registration.");
                return Page();
            }
        }

        private async Task<IList<AuthenticationScheme>> GetExternalLoginsAsync()
        {
            return (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        private async Task<bool> IsEmailAlreadyRegistered()
        {
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            return existingUser != null;
        }

        private async Task SetUserCredentials(User user)
        {
            await _userStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
        }

        private async Task<IActionResult> TryRegisterUser(User user, string returnUrl)
        {
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                return Page();
            }

            _logger.LogInformation("User created a new account with password.");
            return await HandleSuccessfulRegistration(user, returnUrl);
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private async Task<IActionResult> HandleSuccessfulRegistration(User user, string returnUrl)
        {
            if (_userManager.Options.SignIn.RequireConfirmedAccount)
            {
                return await HandleEmailConfirmation(user, returnUrl);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        private async Task<IActionResult> HandleEmailConfirmation(User user, string returnUrl)
        {
            var email = await _userManager.GetEmailAsync(user);
            return RedirectToPage("RegisterConfirmation", new { email, returnUrl });
        }

        private void LogRegistrationError(Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for email: {Email}", Input.Email);
        }

        private User CreateUser()
        {
            try
            {
                return Activator.CreateInstance<User>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(User)}'. " +
                    $"Ensure that '{nameof(User)}' is not an abstract class and has a parameterless constructor.");
            }
        }

        private IUserEmailStore<User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<User>)_userStore;
        }
    }
}