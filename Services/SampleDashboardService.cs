using Amanat.Models;
using Amanat.ViewModels;

namespace Amanat.Services;

public sealed class SampleDashboardService : IDashboardService
{
    private static DashboardShell BuildShell(ApplicationUser user, string role, string fallbackName, string active)
    {
        var displayName = string.IsNullOrWhiteSpace(user.FullName) ? fallbackName : user.FullName;
        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var initials = parts.Length == 0
            ? "AM"
            : string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));

        return new DashboardShell
        {
            Role = role,
            FullName = displayName,
            Initials = initials,
            ActiveLink = active,
            SearchPlaceholder = role == AmanatRoles.Treasurer ? "Search transactions..." : "Search campaigns...",
            ShowSearchOnSmallScreens = role != AmanatRoles.Organizer,
            ShowTagline = role == AmanatRoles.Organizer,
            ShowRoleHeaderLink = role != AmanatRoles.Organizer
        };
    }

    public Task<ContributorDashboardViewModel> GetContributorAsync(ApplicationUser user)
    {
        var model = new ContributorDashboardViewModel
        {
            Shell = BuildShell(user, AmanatRoles.Contributor, "Hamza Raza", "pledges"),
            Stats =
            [
                new StatCard { Label = "Total Pledged", Value = "PKR 25,000", Icon = "bi-cash-stack", IconColor = "text-forest", SmallValue = true },
                new StatCard { Label = "Total Contributed", Value = "PKR 18,000", Icon = "bi-check2-circle", IconColor = "#047857", SmallValue = true },
                new StatCard { Label = "Pending Payments", Value = "PKR 7,000", Icon = "bi-hourglass-split", IconColor = "#B45309", ValueStyle = "color: var(--amanat-amber);", SmallValue = true },
                new StatCard { Label = "Active Pledges", Value = "2", Icon = "bi-collection", IconColor = "text-forest" }
            ],
            Pledges =
            [
                new PledgeCard
                {
                    CampaignTitle = "Clean Water Initiative",
                    ImagePath = "assets/images/thumb-water.jpg",
                    ImageAlt = "Clean Water Initiative community well construction",
                    Description = "Providing clean drinking water for underserved communities.",
                    Status = "Verified",
                    IsVerified = true,
                    RaisedText = "PKR 75,000 raised",
                    TargetText = "Target: PKR 100,000",
                    Percent = 75,
                    YourPledgeText = "Your Pledge: PKR 10,000",
                    PaymentText = "Paid: PKR 10,000 (Fully Settled)",
                    PaymentStyle = "color:#15803D;",
                    ButtonText = "View Campaign Ledger",
                    ButtonTarget = "/Campaigns/Details/clean-water-initiative"
                },
                new PledgeCard
                {
                    CampaignTitle = "Education for All",
                    ImagePath = "assets/images/thumb-education.jpg",
                    ImageAlt = "Education for All classroom refurbishment",
                    Description = "Supporting quality education and classroom supplies for every child.",
                    Status = "Pending Due",
                    IsVerified = false,
                    RaisedText = "PKR 150,000 raised",
                    TargetText = "Target: PKR 250,000",
                    Percent = 60,
                    YourPledgeText = "Your Pledge: PKR 15,000",
                    PaymentText = "Paid: PKR 8,000 • Remaining: PKR 7,000",
                    PaymentStyle = "color:#B45309;",
                    ButtonText = "+ Record Reference",
                    ButtonTarget = "#record-reference-section",
                    ButtonOutline = false
                }
            ],
            Contributions =
            [
                new ContributionRow { Campaign = "Clean Water Initiative", DateText = "Mar 02, 2025", Reference = "TRX78452", AmountText = "PKR 10,000", Status = "Verified" },
                new ContributionRow { Campaign = "Education for All", DateText = "Feb 18, 2025", Reference = "TRX78431", AmountText = "PKR 8,000", Status = "Verified" },
                new ContributionRow { Campaign = "Medical Aid Fund", DateText = "Jan 27, 2025", Reference = "TRX78390", AmountText = "PKR 5,000", Status = "Pending" }
            ]
        };

        return Task.FromResult(model);
    }

    public Task<OrganizerDashboardViewModel> GetOrganizerAsync(ApplicationUser user)
    {
        var model = new OrganizerDashboardViewModel
        {
            Shell = BuildShell(user, AmanatRoles.Organizer, "Ayesha Khan", "overview"),
            Stats =
            [
                new StatCard { Label = "Total Campaigns", Value = "5", Icon = "bi-clipboard", IconColor = "text-forest" },
                new StatCard { Label = "Total Raised", Value = "PKR 375,000", Icon = "bi-cash-coin", IconColor = "text-light-green", SmallValue = true },
                new StatCard { Label = "Pending Approvals", Value = "2", Icon = "bi-exclamation-circle", IconColor = "text-warning", ValueStyle = "color: var(--amanat-amber);", BadgeText = "Requires Action", BadgeClass = "badge-pending" },
                new StatCard { Label = "Active Campaigns", Value = "3", Icon = "bi-lightning-charge", IconColor = "text-forest", ValueStyle = "color: var(--amanat-forest);", BadgeText = "Live", BadgeClass = "badge-verified" }
            ],
            RecentCampaigns =
            [
                new MiniCampaign { Title = "Clean Water Initiative", Percent = 75, TargetText = "Target: PKR 100K", Icon = "bi-droplet", IconStyle = "background:#F0FDFA; border:1px solid #CCFBF1; color:#0F766E;" },
                new MiniCampaign { Title = "Education for All", Percent = 60, TargetText = "Target: PKR 250K", Icon = "bi-book", IconStyle = "background:#FFFBEB; border:1px solid #FEF3C7; color:#B45309;" },
                new MiniCampaign { Title = "Medical Aid Fund", Percent = 100, TargetText = "Target: PKR 150K", Icon = "bi-heart-pulse", IconStyle = "background:#FFF1F2; border:1px solid #FFE4E6; color:#BE123C;" }
            ],
            Approvals =
            [
                new ApprovalRow { Id = "#REC-4122", Attachment = "Attached: Receipt.pdf", Vendor = "Rawal Electric Services", Detail = "School wiring materials", Campaign = "Education for All", CampaignBadgeClass = "badge-pending", AmountText = "PKR 12,500", Status = "Pending Signoff" },
                new ApprovalRow { Id = "#REC-4125", Attachment = "Attached: Invoice_Water.pdf", Vendor = "Al-Madina Water Supplies", Detail = "Filtration pipes & pump setup", Campaign = "Clean Water Initiative", CampaignBadgeClass = "badge-verified", AmountText = "PKR 8,400", Status = "Pending Signoff" }
            ]
        };

        return Task.FromResult(model);
    }

    public Task<TreasurerDashboardViewModel> GetTreasurerAsync(ApplicationUser user)
    {
        var model = new TreasurerDashboardViewModel
        {
            Shell = BuildShell(user, AmanatRoles.Treasurer, "Tariq Saeed", "verifications"),
            Stats =
            [
                new StatCard { Label = "Total Received", Value = "PKR 285,000", Icon = "bi-cash-coin", IconColor = "#2563EB", SmallValue = true },
                new StatCard { Label = "Pending Verifications", Value = "12", Icon = "bi-hourglass-split", IconColor = "#D97706", ValueStyle = "color: var(--amanat-amber);" },
                new StatCard { Label = "Total Expenses", Value = "PKR 120,000", Icon = "bi-wallet2", IconColor = "#E11D48", SmallValue = true },
                new StatCard { Label = "Approved Expenses", Value = "PKR 95,000", Icon = "bi-patch-check", IconColor = "#059669", SmallValue = true }
            ],
            Verifications =
            [
                new VerificationRow { Contributor = "Ali Raza", AmountText = "PKR 5,000", Reference = "TRX78452" },
                new VerificationRow { Contributor = "Sara Khan", AmountText = "PKR 10,000", Reference = "TRX78451" },
                new VerificationRow { Contributor = "Ahmed Raza", AmountText = "PKR 2,500", Reference = "TRX78450" },
                new VerificationRow { Contributor = "Fatima Noor", AmountText = "PKR 15,000", Reference = "TRX78449" }
            ]
        };

        return Task.FromResult(model);
    }
}
