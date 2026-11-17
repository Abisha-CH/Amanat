namespace Amanat.Models;

public sealed class StatCard
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string NoteIcon { get; set; } = string.Empty;
    public string IconBackground { get; set; } = "#EFF4EF";
    public string IconColor { get; set; } = "var(--amanat-forest)";
    public string BadgeText { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = "badge-verified";
    public string ValueStyle { get; set; } = string.Empty;
    public bool SmallValue { get; set; }
}

public sealed class PledgeCard
{
    public string CampaignTitle { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string ImageAlt { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string RaisedText { get; set; } = string.Empty;
    public string TargetText { get; set; } = string.Empty;
    public int Percent { get; set; }
    public string YourPledgeText { get; set; } = string.Empty;
    public string PaymentText { get; set; } = string.Empty;
    public string PaymentStyle { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public string ButtonTarget { get; set; } = string.Empty;
    public bool ButtonOutline { get; set; } = true;
}

public sealed class MiniCampaign
{
    public string Title { get; set; } = string.Empty;
    public int Percent { get; set; }
    public string TargetText { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string IconStyle { get; set; } = string.Empty;
}

public sealed class ContributionRow
{
    public string Campaign { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class VerificationRow
{
    public string Contributor { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
}

public sealed class ApprovalRow
{
    public string Id { get; set; } = string.Empty;
    public string Attachment { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Campaign { get; set; } = string.Empty;
    public string CampaignBadgeClass { get; set; } = "badge-pending";
    public string AmountText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class DashboardShell
{
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string ActiveLink { get; set; } = string.Empty;
    public string SearchPlaceholder { get; set; } = string.Empty;
    public bool ShowSearchOnSmallScreens { get; set; } = true;
    public bool ShowTagline { get; set; }
    public bool ShowRoleHeaderLink { get; set; } = true;
}
