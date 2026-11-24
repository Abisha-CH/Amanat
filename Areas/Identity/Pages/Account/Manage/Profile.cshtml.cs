using Amanat.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Amanat.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string Email { get; set; } = string.Empty;

    public string RoleText { get; set; } = string.Empty;

    public string MemberSince { get; set; } = string.Empty;

    public string? StatusMessage { get; set; }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Please enter your full name.")]
        [StringLength(100, ErrorMessage = "Full name must be at most 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        public string? PhoneNumber { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        Input.FullName = user.FullName;
        Input.PhoneNumber = user.PhoneNumber;
        await LoadAccountInfoAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.FullName))
        {
            ModelState.AddModelError("Input.FullName", "Please enter your full name.");
        }

        if (!ModelState.IsValid)
        {
            var current = await _userManager.GetUserAsync(User);
            if (current is null)
            {
                return Challenge();
            }

            await LoadAccountInfoAsync(current);
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        user.FullName = Input.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAccountInfoAsync(user);
            return Page();
        }

        Input.FullName = user.FullName;
        Input.PhoneNumber = user.PhoneNumber;
        StatusMessage = "Your profile has been updated.";
        await LoadAccountInfoAsync(user);
        return Page();
    }

    private async Task LoadAccountInfoAsync(ApplicationUser user)
    {
        Email = user.Email ?? user.UserName ?? string.Empty;
        var roles = await _userManager.GetRolesAsync(user);
        RoleText = roles.Count == 0 ? "Member" : string.Join(", ", roles);
        MemberSince = user.CreatedAt.ToString("MMMM d, yyyy");
    }
}
