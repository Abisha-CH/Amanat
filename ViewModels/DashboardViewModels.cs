using Amanat.Models;

namespace Amanat.ViewModels;

public sealed class ContributorDashboardViewModel
{
    public required DashboardShell Shell { get; set; }
    public IReadOnlyList<StatCard> Stats { get; set; } = [];
    public IReadOnlyList<PledgeCard> Pledges { get; set; } = [];
    public IReadOnlyList<ContributionRow> Contributions { get; set; } = [];
    public IReadOnlyList<Campaign> CampaignOptions { get; set; } = [];
    public PaymentReferenceInput Reference { get; set; } = new();
}

public sealed class OrganizerDashboardViewModel
{
    public required DashboardShell Shell { get; set; }
    public IReadOnlyList<StatCard> Stats { get; set; } = [];
    public IReadOnlyList<MiniCampaign> RecentCampaigns { get; set; } = [];
    public IReadOnlyList<ApprovalRow> Approvals { get; set; } = [];
    public IReadOnlyList<Campaign> CampaignOptions { get; set; } = [];
    public CampaignCreateInput NewCampaign { get; set; } = new();
    public CampaignUpdateInput NewUpdate { get; set; } = new();
}

public sealed class TreasurerDashboardViewModel
{
    public required DashboardShell Shell { get; set; }
    public IReadOnlyList<StatCard> Stats { get; set; } = [];
    public IReadOnlyList<VerificationRow> Verifications { get; set; } = [];
    public IReadOnlyList<Campaign> CampaignOptions { get; set; } = [];
    public ExpenseInput Expense { get; set; } = new();
}
