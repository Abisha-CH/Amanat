using Amanat.Models;

namespace Amanat.Services;

public sealed class SampleLedgerService : ILedgerService
{
    private static readonly IReadOnlyList<LedgerFund> Funds =
    [
        new() { Name = "Oakridge STEM Initiative", FundId = "#OK-2025-419", FocusArea = "Education", FocusKey = "education", Inflow = 31840m, Outflow = 18420m, Reserve = 13420m, Seal = "9c02...e3bb4", Status = "Sealed", IsSealed = true },
        new() { Name = "Green Crescent Water Trust", FundId = "#GC-2025-118", FocusArea = "Clean Water", FocusKey = "water", Inflow = 1120000m, Outflow = 742600m, Reserve = 377400m, Seal = "4f81...a72c", Status = "Sealed", IsSealed = true },
        new() { Name = "Al-Noor Medical Relief Fund", FundId = "#AN-2025-204", FocusArea = "Healthcare", FocusKey = "health", Inflow = 890500m, Outflow = 610250m, Reserve = 280250m, Seal = "b310...07df", Status = "Sealed", IsSealed = true },
        new() { Name = "Bright Futures Scholarship Fund", FundId = "#BF-2025-076", FocusArea = "Campus Welfare", FocusKey = "welfare", Inflow = 645000m, Outflow = 418900m, Reserve = 226100m, Seal = "7e29...910c", Status = "In Review", IsSealed = false },
        new() { Name = "Neighbourhood Community Aid Pool", FundId = "#NA-2025-331", FocusArea = "Community Aid", FocusKey = "aid", Inflow = 428700m, Outflow = 389100m, Reserve = 39600m, Seal = "2d94...5b81", Status = "Sealed", IsSealed = true }
    ];

    private static readonly IReadOnlyList<CashbookEntry> Entries =
    [
        new() { DateText = "Mar 14, 2025", Reference = "TX#0x8a9f...32d1", Party = "Pacific Civic Foundation", Detail = "STEM Expansion Q1 Grant", Category = "Institutional Grant", CategoryKey = "civic", InflowText = "+Rs. 15,000.00", OutflowText = "—", ProofIcon = "bi-file-earmark-text", ProofFile = "Grant_Agr.pdf", AuditStamp = "NVance (Sig 1/2)", Status = "Verified" },
        new() { DateText = "Mar 12, 2025", Reference = "TX#0x4c2b...99e0", Party = "LabQuest Robotics Corp", Detail = "Modular Sensor Kits (x12)", Category = "Hardware Kits", CategoryKey = "hardware", InflowText = "—", OutflowText = "-Rs. 3,450.00", ProofIcon = "bi-receipt", ProofFile = "INV-8891.pdf", AuditStamp = "2/2 Multisig Conf", Status = "Disbursed" },
        new() { DateText = "Mar 10, 2025", Reference = "TX#0x117a...bbf4", Party = "AutoDesk Cloud Education", Detail = "Annual Fusion360 Lab Seats", Category = "Software Tools", CategoryKey = "software", InflowText = "—", OutflowText = "-Rs. 1,200.00", ProofIcon = "bi-receipt", ProofFile = "AD_Invoice.pdf", AuditStamp = "2/2 Multisig Conf", Status = "Disbursed" },
        new() { DateText = "Mar 08, 2025", Reference = "TX#0x65bc...01fa", Party = "Community Micro-Backers Pool", Detail = "Oakridge Neighborhood Day Drive (34 Pledges)", Category = "Civic Pledges", CategoryKey = "civic", InflowText = "+Rs. 4,840.00", OutflowText = "—", ProofIcon = "bi-graph-up", ProofFile = "PledgeBatch.csv", AuditStamp = "Stripe Connect", Status = "Verified" },
        new() { DateText = "Mar 05, 2025", Reference = "TX#0x228e...981c", Party = "Dr. Maya Lin (Guest Lecturer)", Detail = "Biotech Immersion Workshop Stipend", Category = "Faculty Stipends", CategoryKey = "faculty", InflowText = "—", OutflowText = "-Rs. 850.00", ProofIcon = "bi-person-badge", ProofFile = "W9_Receipt.pdf", AuditStamp = "2/2 Multisig Conf", Status = "Disbursed" },
        new() { DateText = "Mar 01, 2025", Reference = "TX#0x991f...aa10", Party = "Oakridge Community Center", Detail = "Room 4B & Maker Annex Monthly Sublease", Category = "Facility Lease", CategoryKey = "facility", InflowText = "—", OutflowText = "-Rs. 950.00", ProofIcon = "bi-house-door", ProofFile = "Lease_Rec.pdf", AuditStamp = "Auto-standing order", Status = "Scheduled", IsPending = true }
    ];

    private static readonly IReadOnlyList<PendingApproval> PendingApprovals =
    [
        new() { Title = "MakerBot 3D Printer Filament (8 Spools)", Reference = "EXP-2025-081", Icon = "bi-printer", Submitter = "Marcus Vance (Lab Mentor)", SubmittedText = "Submitted 2h ago", Attachment = "Invoice_MakerBot.pdf (842 KB)", AmountText = "Rs. 284.50" },
        new() { Title = "Circuit Board PCB Prototype Fabrication", Reference = "EXP-2025-082", Icon = "bi-cpu", Submitter = "Aria Chen (Hardware Lead)", SubmittedText = "Submitted 5h ago", Attachment = "JLCPCB_Receipt_883.pdf (1.2 MB)", AmountText = "Rs. 612.00" },
        new() { Title = "Workshop Safety Goggles (Bulk Pack of 30)", Reference = "EXP-2025-083", Icon = "bi-shield-check", Submitter = "Julian Ross (Lab Manager)", SubmittedText = "Submitted Yesterday", Attachment = "Safety_Goggles_INV.pdf (640 KB)", AmountText = "Rs. 145.20" }
    ];

    public string PublicSealDigest => "9c02a7be...e3bb4";

    public Task<IReadOnlyList<LedgerFund>> GetInstitutionalFundsAsync() => Task.FromResult(Funds);

    public Task<IReadOnlyList<CashbookEntry>> GetCashbookEntriesAsync() => Task.FromResult(Entries);

    public Task<IReadOnlyList<PendingApproval>> GetPendingApprovalsAsync() => Task.FromResult(PendingApprovals);
}
