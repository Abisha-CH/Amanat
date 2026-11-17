using Amanat.Models;

namespace Amanat.Services;

public sealed class SampleCampaignService : ICampaignService
{
    private static readonly IReadOnlyList<Campaign> Campaigns =
    [
        new()
        {
            Slug = "clean-water-initiative",
            Title = "Clean Water Initiative",
            Organization = "Environmental Student Society",
            Category = "Clean Water",
            FocusKey = "water",
            Thumbnail = "assets/images/campaign-water.jpg",
            ThumbnailAlt = "Clean Water Initiative Campus Project",
            ThumbLabel = "Infrastructure",
            HomeThumbnail = "assets/images/campaign-water-home.jpg",
            HomeThumbnailAlt = "Historic community building and water facility",
            Summary = "Installing commercial reverse-osmosis filtration units in underprivileged student resident sectors with live water test reports.",
            HomeSummary = "Installing solar-powered reverse osmosis filtration units for underserved communities.",
            DetailsImage = "assets/images/campaign-water.jpg",
            DetailsImageAlt = "Clean water initiative filtration project",
            Raised = 75000m,
            Goal = 100000m,
            Contributors = 142,
            ApprovedCount = 8,
            ApprovedLabel = "Bills Approved",
            LedgerNote = "PKR 48,000 Disbursed to Vendors",
            StartedOn = new DateTime(2025, 10, 1),
            Deadline = new DateTime(2025, 12, 31)
        },
        new()
        {
            Slug = "education-for-all",
            Title = "Education for All",
            Organization = "FAST Civic Action Club",
            Category = "Education",
            FocusKey = "education",
            Thumbnail = "assets/images/campaign-education.jpg",
            ThumbnailAlt = "Education for All Initiative",
            ThumbLabel = "Education",
            HomeThumbnail = "assets/images/campaign-education-home.jpg",
            HomeThumbnailAlt = "School children and learning supplies",
            Summary = "Direct sponsorship of textbook bundles, classroom desk rehabilitation, and tuition aid for underprivileged children.",
            HomeSummary = "Supporting quality education, school supplies, and classroom renovation for underprivileged kids.",
            DetailsImage = "assets/images/campaign-education-hero.jpg",
            DetailsImageAlt = "Students and classroom supplies for Education for All",
            Raised = 150000m,
            Goal = 250000m,
            Contributors = 210,
            ApprovedCount = 12,
            ApprovedLabel = "Invoices Cleared",
            LedgerNote = "School supplies & desk repairs audited",
            StartedOn = new DateTime(2025, 9, 15),
            Deadline = new DateTime(2025, 11, 30)
        },
        new()
        {
            Slug = "medical-aid-fund",
            Title = "Medical Aid Fund",
            Organization = "Red Crescent Youth Chapter",
            Category = "Healthcare",
            FocusKey = "health",
            Thumbnail = "assets/images/campaign-medical.jpg",
            ThumbnailAlt = "Medical Aid Fund",
            ThumbLabel = "Healthcare",
            HomeThumbnail = "assets/images/campaign-medical-home.jpg",
            HomeThumbnailAlt = "Medical emergency equipment",
            Summary = "Emergency hospitalization grants and life-saving prescription coverage for non-teaching campus staff and workers.",
            HomeSummary = "Emergency medical assistance and essential prescription subsidy for low-income university students.",
            DetailsImage = "assets/images/campaign-medical.jpg",
            DetailsImageAlt = "Medical aid fund emergency assistance",
            Raised = 95000m,
            Goal = 120000m,
            Contributors = 88,
            ApprovedCount = 5,
            ApprovedLabel = "Verified Receipts",
            LedgerNote = "Direct Pharmacy Wire Settlements",
            StartedOn = new DateTime(2025, 10, 20),
            Deadline = new DateTime(2025, 12, 15)
        },
        new()
        {
            Slug = "community-library-project",
            Title = "Community Library Project",
            Organization = "Literary & Debating Guild",
            Category = "Campus Welfare",
            FocusKey = "welfare",
            Thumbnail = "assets/images/campaign-library.jpg",
            ThumbnailAlt = "Community Library Project",
            ThumbLabel = "Campus Welfare",
            HomeThumbnail = "assets/images/campaign-library-home.jpg",
            HomeThumbnailAlt = "Library shelves and books",
            Summary = "Procurement of reference manuals, open-access study terminals, and solar inverter backup for public hall.",
            HomeSummary = "Open-access textbook repository and reading area setup for civic self-study circles.",
            DetailsImage = "assets/images/campaign-library.jpg",
            DetailsImageAlt = "Community library project books and reading area",
            Raised = 45000m,
            Goal = 50000m,
            Contributors = 65,
            ApprovedCount = 4,
            ApprovedLabel = "Equipment Invoices",
            LedgerNote = "Direct Supplier Payment Slips",
            StartedOn = new DateTime(2025, 8, 1),
            Deadline = new DateTime(2025, 10, 31)
        }
    ];

    private static readonly IReadOnlyList<Receipt> Receipts =
    [
        new()
        {
            Title = "Classroom Desks & Whiteboards",
            ImagePath = "assets/images/receipt-desks.jpg",
            ImageAlt = "Receipt for classroom desks",
            Reference = "#REC-4109",
            Vendor = "Vendor: Master Woodcraft H-9",
            AmountText = "PKR 75,000",
            IsVerified = true
        },
        new()
        {
            Title = "School Stationery & Textbook Bundles",
            ImagePath = "assets/images/receipt-stationery.jpg",
            ImageAlt = "Receipt for textbooks and school stationery",
            Reference = "#REC-4110",
            Vendor = "Vendor: National Book Foundation",
            AmountText = "PKR 35,000",
            IsVerified = true
        },
        new()
        {
            Title = "Room Electrical & Fan Maintenance",
            ImagePath = "assets/images/receipt-electrical.jpg",
            ImageAlt = "Receipt for electrical maintenance",
            Reference = "#REC-4122",
            Vendor = "Vendor: Rawal Electric Services",
            AmountText = "PKR 12,500",
            IsVerified = false
        }
    ];

    public Task<IReadOnlyList<Campaign>> GetAllAsync() => Task.FromResult(Campaigns);

    public Task<Campaign?> GetBySlugAsync(string slug) =>
        Task.FromResult(Campaigns.FirstOrDefault(c => c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Receipt>> GetReceiptsAsync(string slug) => Task.FromResult(Receipts);
}
