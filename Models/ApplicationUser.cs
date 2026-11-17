using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Amanat.Models;

public sealed class ApplicationUser : IdentityUser
{
    [PersonalData]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [PersonalData]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class AmanatRoles
{
    public const string Organizer = "Organizer";
    public const string Treasurer = "Treasurer";
    public const string Contributor = "Contributor";

    public static readonly string[] All = [Organizer, Treasurer, Contributor];
}
