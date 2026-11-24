using Amanat.Models;
using Amanat.Models.Entities;
using Amanat.Repositories.Interfaces;
using Amanat.ViewModels;

namespace Amanat.Services;

public sealed class SqlDashboardService : IDashboardService
{
    private const string UnavailableMessage = "Dashboard information is temporarily unavailable. Please try again.";
    private readonly ICampaignRepository _campaigns;
    private readonly IContributionRepository _contributions;
    private readonly IExpenseRepository _expenses;
    private readonly ILedgerRepository _ledger;

    public SqlDashboardService(
        ICampaignRepository campaigns,
        IContributionRepository contributions,
        IExpenseRepository expenses,
        ILedgerRepository ledger)
    {
        _campaigns = campaigns;
        _contributions = contributions;
        _expenses = expenses;
        _ledger = ledger;
    }

    public Task<ContributorDashboardViewModel> GetContributorAsync(ApplicationUser user) =>
        SqlServiceGuard.RunAsync(async () =>
        {
            var campaignEntities = await _campaigns.GetAllAsync(false);
            var campaigns = await DisplayCampaignsAsync(campaignEntities);
            var campaignById = campaignEntities.ToDictionary(c => c.CampaignId);
            var contributions = await _contributions.GetByContributorAsync(user.Id);
            var rows = contributions
                .Select(c => DomainMapper.ToContributionRow(
                    c,
                    campaignById.TryGetValue(c.CampaignId, out var campaign) ? campaign.Title : "Campaign"))
                .ToList();

            var totalPledged = contributions.Sum(c => c.Amount);
            var totalContributed = contributions.Where(IsVerified).Sum(c => c.Amount);
            var pendingPayments = contributions.Where(IsPending).Sum(c => c.Amount);
            var pledgeCards = new List<PledgeCard>();
            var priority = contributions
                .Where(c => IsPending(c) || IsVerified(c))
                .OrderByDescending(c => c.SubmittedAtUtc)
                .Select(c => c.CampaignId)
                .Distinct()
                .Concat(campaignEntities.Select(c => c.CampaignId))
                .Distinct()
                .Take(2);

            foreach (var campaignId in priority)
            {
                if (!campaignById.TryGetValue(campaignId, out var entity)) continue;
                var campaign = campaigns.First(c => c.Slug == entity.Slug);
                var pledge = contributions.Where(c => c.CampaignId == campaignId).OrderByDescending(c => c.SubmittedAtUtc).FirstOrDefault();
                var verified = pledge is not null && IsVerified(pledge);
                var paid = verified ? pledge!.Amount : contributions.Where(c => c.CampaignId == campaignId && IsVerified(c)).Sum(c => c.Amount);
                var remaining = (pledge?.Amount ?? 0m) - paid;

                pledgeCards.Add(new PledgeCard
                {
                    CampaignTitle = campaign.Title,
                    ImagePath = string.IsNullOrWhiteSpace(campaign.HomeThumbnail) ? campaign.Thumbnail : campaign.HomeThumbnail,
                    ImageAlt = string.IsNullOrWhiteSpace(campaign.HomeThumbnailAlt) ? campaign.HomeThumbnailAlt : campaign.HomeThumbnailAlt,
                    Description = string.IsNullOrWhiteSpace(campaign.HomeSummary) ? campaign.Summary : campaign.HomeSummary,
                    Status = pledge?.Status ?? "Open",
                    IsVerified = verified,
                    RaisedText = campaign.RaisedText + " raised",
                    TargetText = "Target: " + campaign.GoalText,
                    Percent = campaign.ProgressPercent,
                    YourPledgeText = pledge is null ? "No pledge yet" : "Your Pledge: " + Amanat.Models.Campaign.AmountText(pledge.Amount),
                    PaymentText = verified
                        ? "Paid: " + Amanat.Models.Campaign.AmountText(paid) + " (Fully Settled)"
                        : remaining > 0m
                            ? "Paid: " + Amanat.Models.Campaign.AmountText(paid) + " • Remaining: " + Amanat.Models.Campaign.AmountText(remaining)
                            : "Awaiting treasurer verification",
                    PaymentStyle = verified ? "color:#15803D;" : "color:#B45309;",
                    ButtonText = verified ? "View Campaign Ledger" : "+ Record Reference",
                    ButtonTarget = verified ? "/Campaigns/Details/" + campaign.Slug : "#record-reference-section",
                    ButtonOutline = verified
                });
            }

            return new ContributorDashboardViewModel
            {
                Shell = BuildShell(user, AmanatRoles.Contributor, "pledges"),
                Stats =
                [
                    new StatCard { Label = "Total Pledged", Value = Amanat.Models.Campaign.AmountText(totalPledged), Icon = "bi-cash-stack", IconColor = "text-forest", SmallValue = true },
                    new StatCard { Label = "Total Contributed", Value = Amanat.Models.Campaign.AmountText(totalContributed), Icon = "bi-check2-circle", IconColor = "#047857", SmallValue = true },
                    new StatCard { Label = "Pending Payments", Value = Amanat.Models.Campaign.AmountText(pendingPayments), Icon = "bi-hourglass-split", IconColor = "#B45309", ValueStyle = "color: var(--amanat-amber);", SmallValue = true },
                    new StatCard { Label = "Active Pledges", Value = contributions.Count(c => IsPending(c) || IsVerified(c)).ToString(), Icon = "bi-collection", IconColor = "text-forest" }
                ],
                Pledges = pledgeCards,
                Contributions = rows,
                CampaignOptions = campaigns
            };
        }, UnavailableMessage);

    public Task<OrganizerDashboardViewModel> GetOrganizerAsync(ApplicationUser user) =>
        SqlServiceGuard.RunAsync(async () =>
        {
            var allEntities = await _campaigns.GetAllAsync(false);
            var ownedEntities = allEntities.Where(c => c.CreatedByUserId == user.Id).ToList();
            var campaigns = await DisplayCampaignsAsync(ownedEntities);
            var campaignById = ownedEntities.ToDictionary(c => c.CampaignId);
            var pendingExpenses = (await _expenses.GetPendingApprovalsAsync())
                .Where(e => campaignById.ContainsKey(e.CampaignId))
                .ToList();
            var approvals = new List<ApprovalRow>();
            foreach (var expense in pendingExpenses)
            {
                var campaign = campaignById[expense.CampaignId];
                var receipt = await _ledger.GetReceiptForExpenseAsync(expense.ExpenseId);
                approvals.Add(DomainMapper.ToApprovalRow(
                    expense,
                    campaign.Title,
                    campaign.FocusKey,
                    receipt?.OriginalFileName ?? receipt?.FileName));
            }

            var recent = ownedEntities
                .Select(entity => new
                {
                    Entity = entity,
                    Display = campaigns.First(c => c.Slug == entity.Slug)
                })
                .OrderByDescending(item => item.Display.ProgressPercent)
                .ThenBy(item => item.Display.Deadline)
                .Take(3)
                .Select(item => new MiniCampaign
                {
                    CampaignId = item.Entity.CampaignId,
                    RowVersion = Convert.ToBase64String(item.Entity.RowVersion),
                    Title = item.Display.Title,
                    Percent = item.Display.ProgressPercent,
                    TargetText = "Target: " + CompactTarget(item.Display.Goal),
                    Icon = IconFor(item.Display.FocusKey),
                    IconStyle = IconStyleFor(item.Display.FocusKey)
                })
                .ToList();

            return new OrganizerDashboardViewModel
            {
                Shell = BuildShell(user, AmanatRoles.Organizer, "overview"),
                Stats =
                [
                    new StatCard { Label = "Total Campaigns", Value = campaigns.Count.ToString(), Icon = "bi-clipboard", IconColor = "text-forest" },
                    new StatCard { Label = "Total Raised", Value = Amanat.Models.Campaign.AmountText(campaigns.Sum(c => c.Raised)), Icon = "bi-cash-coin", IconColor = "text-light-green", SmallValue = true },
                    new StatCard { Label = "Pending Approvals", Value = approvals.Count.ToString(), Icon = "bi-exclamation-circle", IconColor = "text-warning", ValueStyle = "color: var(--amanat-amber);", BadgeText = "Requires Action", BadgeClass = "badge-pending" },
                    new StatCard { Label = "Active Campaigns", Value = ownedEntities.Count(c => c.Status == "Active").ToString(), Icon = "bi-lightning-charge", IconColor = "text-forest", ValueStyle = "color: var(--amanat-forest);", BadgeText = "Live", BadgeClass = "badge-verified" }
                ],
                RecentCampaigns = recent,
                Approvals = approvals,
                CampaignOptions = campaigns
            };
        }, UnavailableMessage);

    public Task<TreasurerDashboardViewModel> GetTreasurerAsync(ApplicationUser user) =>
        SqlServiceGuard.RunAsync(async () =>
        {
            var campaignEntities = await _campaigns.GetAllAsync(false);
            var campaigns = await DisplayCampaignsAsync(campaignEntities);
            var campaignById = campaignEntities.ToDictionary(c => c.CampaignId);
            var pendingContributions = await _contributions.GetPendingAsync();
            var summary = await _ledger.GetPublicSummaryAsync();

            return new TreasurerDashboardViewModel
            {
                Shell = BuildShell(user, AmanatRoles.Treasurer, "verifications"),
                Stats =
                [
                    new StatCard { Label = "Total Received", Value = Amanat.Models.Campaign.AmountText(summary.TotalVerifiedContributions), Icon = "bi-cash-coin", IconColor = "#2563EB", SmallValue = true },
                    new StatCard { Label = "Pending Verifications", Value = pendingContributions.Count.ToString(), Icon = "bi-hourglass-split", IconColor = "#D97706", ValueStyle = "color: var(--amanat-amber);" },
                    new StatCard { Label = "Total Expenses", Value = Amanat.Models.Campaign.AmountText(summary.TotalApprovedExpenses), Icon = "bi-wallet2", IconColor = "#E11D48", SmallValue = true },
                    new StatCard { Label = "Approved Expenses", Value = Amanat.Models.Campaign.AmountText(summary.TotalApprovedExpenses), Icon = "bi-patch-check", IconColor = "#059669", SmallValue = true }
                ],
                Verifications = pendingContributions.Select(DomainMapper.ToVerificationRow).ToList(),
                CampaignOptions = campaigns
            };
        }, UnavailableMessage);

    private async Task<List<Campaign>> DisplayCampaignsAsync(IReadOnlyList<CampaignEntity> entities)
    {
        var result = new List<Campaign>(entities.Count);
        foreach (var entity in entities)
        {
            var balance = await _ledger.GetCampaignBalanceAsync(entity.CampaignId);
            result.Add(DomainMapper.ToCampaign(entity, balance));
        }

        return result;
    }

    private static DashboardShell BuildShell(ApplicationUser user, string role, string active)
    {
        var displayName = string.IsNullOrWhiteSpace(user.FullName)
            ? (!string.IsNullOrWhiteSpace(user.Email) ? user.Email : user.UserName ?? role)
            : user.FullName;
        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var initials = parts.Length == 0
            ? "AM"
            : string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));

        return new DashboardShell
        {
            Role = role,
            FullName = displayName,
            Email = user.Email ?? user.UserName ?? string.Empty,
            Initials = initials,
            ActiveLink = active,
            SearchPlaceholder = role == AmanatRoles.Treasurer ? "Search transactions..." : "Search campaigns...",
            ShowSearchOnSmallScreens = role != AmanatRoles.Organizer,
            ShowTagline = role == AmanatRoles.Organizer,
            ShowRoleHeaderLink = role != AmanatRoles.Organizer
        };
    }

    private static bool IsVerified(ContributionEntity contribution) =>
        contribution.Status.Equals("Verified", StringComparison.OrdinalIgnoreCase);

    private static bool IsPending(ContributionEntity contribution) =>
        contribution.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase);

    private static string CompactTarget(decimal value) => value >= 1000m
        ? value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).TrimEnd('0', '.') + "K"
        : value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

    private static string IconFor(string focusKey) => focusKey switch
    {
        "water" => "bi-droplet",
        "education" => "bi-book",
        "health" => "bi-heart-pulse",
        _ => "bi-building"
    };

    private static string IconStyleFor(string focusKey) => focusKey switch
    {
        "water" => "background:#F0FDFA; border:1px solid #CCFBF1; color:#0F766E;",
        "education" => "background:#FFFBEB; border:1px solid #FEF3C7; color:#B45309;",
        "health" => "background:#FFF1F2; border:1px solid #FFE4E6; color:#BE123C;",
        _ => "background:#EFF6FF; border:1px solid #BFDBFE; color:#1D4ED8;"
    };
}
