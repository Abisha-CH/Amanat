using Amanat.Data;
using Amanat.Models;
using Amanat.Models.Entities;
using Amanat.Repositories.Interfaces;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace Amanat.Data;

public static class DomainSeeder
{
    private const string OrganizerEmail = "organizer.seed@amanat.test";
    private const string TreasurerEmail = "treasurer.seed@amanat.test";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var scoped = scope.ServiceProvider;
        var campaigns = scoped.GetRequiredService<ICampaignRepository>();
        var contributions = scoped.GetRequiredService<IContributionRepository>();
        var expenses = scoped.GetRequiredService<IExpenseRepository>();
        var connections = scoped.GetRequiredService<IDbConnectionFactory>();
        var users = scoped.GetRequiredService<UserManager<ApplicationUser>>();

        if (await campaigns.GetBySlugAsync("education-for-all") is not null)
        {
            return;
        }

        var organizer = await users.FindByEmailAsync(OrganizerEmail);
        var treasurer = await users.FindByEmailAsync(TreasurerEmail);
        if (organizer is null || treasurer is null)
        {
            return;
        }

        var cleanWater = await campaigns.CreateAsync(NewCampaign(
            "clean-water-initiative", "Clean Water Initiative", "Environmental Student Society",
            "Clean Water", "water", "water",
            "Installing commercial reverse-osmosis filtration units in underprivileged student resident sectors with live water test reports.",
            "Installing solar-powered reverse osmosis filtration units for underserved communities.",
            "PKR 48,000 Disbursed to Vendors", 100000m,
            new DateTime(2025, 10, 1), new DateTime(2025, 12, 31), organizer.Id));

        var education = await campaigns.CreateAsync(NewCampaign(
            "education-for-all", "Education for All", "FAST Civic Action Club",
            "Education", "education", "education",
            "Direct sponsorship of textbook bundles, classroom desk rehabilitation, and tuition aid for underprivileged children.",
            "Supporting quality education, school supplies, and classroom renovation for underprivileged kids.",
            "School supplies & desk repairs audited", 250000m,
            new DateTime(2025, 9, 15), new DateTime(2025, 11, 30), organizer.Id));

        var medical = await campaigns.CreateAsync(NewCampaign(
            "medical-aid-fund", "Medical Aid Fund", "Red Crescent Youth Chapter",
            "Healthcare", "health", "medical",
            "Emergency hospitalization grants and life-saving prescription coverage for non-teaching campus staff and workers.",
            "Emergency medical assistance and essential prescription subsidy for low-income university students.",
            "Direct Pharmacy Wire Settlements", 120000m,
            new DateTime(2025, 10, 20), new DateTime(2025, 12, 15), organizer.Id));

        await campaigns.CreateAsync(NewCampaign(
            "community-library-project", "Community Library Project", "Literary & Debating Guild",
            "Campus Welfare", "welfare", "library",
            "Procurement of reference manuals, open-access study terminals, and solar inverter backup for public hall.",
            "Open-access textbook repository and reading area setup for civic self-study circles.",
            "Direct Supplier Payment Slips", 50000m,
            new DateTime(2025, 8, 1), new DateTime(2025, 10, 31), organizer.Id));

        await SeedContributionAsync(connections, contributions, cleanWater, "Ali Raza", 75000m, "TRX-CW-75000", new DateTime(2025, 10, 18), "Verified", treasurer.Id);
        await SeedContributionAsync(connections, contributions, education, "Sara Khan", 150000m, "TRX-EDU-150000", new DateTime(2025, 10, 12), "Verified", treasurer.Id);
        await SeedContributionAsync(connections, contributions, medical, "Community Donors", 120000m, "TRX-MED-120000", new DateTime(2025, 10, 22), "Verified", treasurer.Id);
        await SeedContributionAsync(connections, contributions, education, "Ahmed Raza", 7000m, "TRX78492", new DateTime(2025, 10, 24), "Pending", null);
        await SeedContributionAsync(connections, contributions, education, "Fatima Noor", 2500m, "TRX78449", new DateTime(2025, 10, 11), "Rejected", treasurer.Id);

        var desks = await SeedExpenseAsync(connections, expenses, education, "Master Woodcraft H-9", 75000m, "#REC-4109", "Classroom Desks & Whiteboards", new DateTime(2025, 10, 21), "Approved", organizer.Id, treasurer.Id);
        var stationery = await SeedExpenseAsync(connections, expenses, education, "National Book Foundation", 35000m, "#REC-4110", "School Stationery & Textbook Bundles", new DateTime(2025, 10, 22), "Approved", organizer.Id, treasurer.Id);
        var water = await SeedExpenseAsync(connections, expenses, cleanWater, "Al-Madina Water Supplies", 8400m, "#REC-4125", "Filtration pipes & pump setup", new DateTime(2025, 10, 23), "Approved", organizer.Id, treasurer.Id);
        await SeedExpenseAsync(connections, expenses, education, "Rawal Electric Services", 12500m, "#REC-4122", "School wiring materials", new DateTime(2025, 10, 25), "Rejected", organizer.Id, treasurer.Id);
        var maker = await SeedExpenseAsync(connections, expenses, education, "MakerBot Store", 284.50m, "EXP-2025-081", "MakerBot 3D Printer Filament (8 Spools)", new DateTime(2025, 11, 26, 9, 0, 0), "Pending", null, treasurer.Id);
        var pcb = await SeedExpenseAsync(connections, expenses, education, "JLCPCB", 612.00m, "EXP-2025-082", "Circuit Board PCB Prototype Fabrication", new DateTime(2025, 11, 26, 14, 0, 0), "Pending", null, treasurer.Id);
        var goggles = await SeedExpenseAsync(connections, expenses, education, "AmazonCommercial", 145.20m, "EXP-2025-083", "Workshop Safety Goggles (Bulk Pack of 30)", new DateTime(2025, 11, 27), "Pending", null, treasurer.Id);

        await SeedReceiptAsync(connections, desks, "REC-4109", "Master Woodcraft H-9", "Classroom Desks & Whiteboards", "receipt-desks.jpg", 75000m, true, treasurer.Id);
        await SeedReceiptAsync(connections, stationery, "REC-4110", "National Book Foundation", "School Stationery & Textbook Bundles", "receipt-stationery.jpg", 35000m, true, treasurer.Id);
        await SeedReceiptAsync(connections, water, "REC-4125", "Al-Madina Water Supplies", "Filtration pipes & pump setup", "receipt-electrical.jpg", 8400m, true, treasurer.Id);
        await SeedReceiptAsync(connections, maker, "INV-2025-081", "MakerBot Store", "Invoice_MakerBot.pdf (842 KB)", "Invoice_MakerBot.pdf", 284.50m, false, treasurer.Id);
        await SeedReceiptAsync(connections, pcb, "INV-2025-082", "JLCPCB", "JLCPCB_Receipt_883.pdf (1.2 MB)", "JLCPCB_Receipt_883.pdf", 612.00m, false, treasurer.Id);
        await SeedReceiptAsync(connections, goggles, "INV-2025-083", "AmazonCommercial", "AmazonCommercial_Inv.pdf (410 KB)", "AmazonCommercial_Inv.pdf", 145.20m, false, treasurer.Id);

        await campaigns.AddUpdateAsync(new CampaignUpdateEntity
        {
            CampaignId = education,
            AuthorUserId = organizer.Id,
            Title = "Desks delivered to H-9",
            Body = "The first batch of refurbished classroom desks has been delivered and photographed by the project team.",
            IsPublic = true,
            PublishedAtUtc = new DateTime(2025, 10, 21),
            CreatedAtUtc = new DateTime(2025, 10, 21)
        });
        await campaigns.AddUpdateAsync(new CampaignUpdateEntity
        {
            CampaignId = education,
            AuthorUserId = organizer.Id,
            Title = "Textbook bundles procured",
            Body = "Textbook bundles are now ready for distribution across the partner learning circles.",
            IsPublic = true,
            PublishedAtUtc = new DateTime(2025, 10, 22),
            CreatedAtUtc = new DateTime(2025, 10, 22)
        });
    }

    private static CampaignEntity NewCampaign(
        string slug, string title, string organization, string category, string focusKey, string imageKey,
        string summary, string homeSummary, string ledgerNote, decimal goal, DateTime startedOn, DateTime deadline,
        string ownerId) => new()
        {
            Slug = slug,
            Title = title,
            Organization = organization,
            Category = category,
            FocusKey = focusKey,
            Thumbnail = "assets/images/campaign-" + imageKey + ".jpg",
            ThumbnailAlt = title,
            ThumbnailLabel = category,
            HomeThumbnail = "assets/images/campaign-" + imageKey + "-home.jpg",
            HomeThumbnailAlt = title,
            DetailsImage = imageKey == "education" ? "assets/images/campaign-education-hero.jpg" : "assets/images/campaign-" + imageKey + ".jpg",
            DetailsImageAlt = title,
            Summary = summary,
            HomeSummary = homeSummary,
            LedgerNote = ledgerNote,
            Goal = goal,
            StartedOn = startedOn,
            Deadline = deadline,
            Status = "Active",
            CreatedByUserId = ownerId
        };

    private static async Task SeedContributionAsync(
        IDbConnectionFactory connections,
        IContributionRepository repository,
        int campaignId,
        string fullName,
        decimal amount,
        string reference,
        DateTime submittedAt,
        string decision,
        string? actorId)
    {
        long id;
        await using (var connection = connections.CreateConnection())
        {
            await connection.OpenAsync();
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            id = await repository.SubmitAsync(new ContributionEntity
            {
                CampaignId = campaignId,
                FullName = fullName,
                Amount = amount,
                TransactionRef = reference,
                DateSent = submittedAt,
                SubmittedAtUtc = submittedAt,
                Notes = "Seeded demonstration record"
            }, transaction);
            await transaction.CommitAsync();
        }

        if (decision == "Pending") return;

        var entity = await repository.GetByIdAsync(id);
        if (entity is null) return;

        await using var writeConnection = connections.CreateConnection();
        await writeConnection.OpenAsync();
        await using var writeTransaction = (SqlTransaction)await writeConnection.BeginTransactionAsync();
        if (decision == "Verified")
        {
            await repository.VerifyAsync(id, actorId!, "Seeded verification", entity.RowVersion, writeTransaction);
        }
        else
        {
            await repository.RejectAsync(id, actorId!, "Seeded rejection", entity.RowVersion, writeTransaction);
        }

        await writeTransaction.CommitAsync();
    }

    private static async Task<long> SeedExpenseAsync(
        IDbConnectionFactory connections,
        IExpenseRepository repository,
        int campaignId,
        string payee,
        decimal amount,
        string reference,
        string description,
        DateTime submittedAt,
        string decision,
        string? decisionByUserId,
        string submitterId)
    {
        long id;
        await using (var connection = connections.CreateConnection())
        {
            await connection.OpenAsync();
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            id = await repository.SubmitAsync(new ExpenseEntity
            {
                CampaignId = campaignId,
                Reference = reference,
                PayeeName = payee,
                Amount = amount,
                Description = description,
                SubmittedAtUtc = submittedAt,
                SubmittedByUserId = submitterId
            }, transaction);
            await transaction.CommitAsync();
        }

        if (decision == "Pending") return id;

        var entity = await repository.GetByIdAsync(id);
        if (entity is null) return id;

        await using var writeConnection = connections.CreateConnection();
        await writeConnection.OpenAsync();
        await using var writeTransaction = (SqlTransaction)await writeConnection.BeginTransactionAsync();
        if (decision == "Approved")
        {
            await repository.ApproveAsync(id, decisionByUserId!, "Seeded approval", entity.RowVersion, writeTransaction);
        }
        else
        {
            await repository.RejectAsync(id, decisionByUserId!, "Seeded rejection", entity.RowVersion, writeTransaction);
        }

        await writeTransaction.CommitAsync();
        return id;
    }

    private static async Task SeedReceiptAsync(
        IDbConnectionFactory connections,
        long expenseId,
        string receiptNumber,
        string vendor,
        string originalFileName,
        string fileName,
        decimal amount,
        bool verified,
        string uploaderId)
    {
        await using var connection = connections.CreateConnection();
        await connection.OpenAsync();
        const string sql = @"
INSERT dbo.ReceiptMetadata
    (ExpenseId, ContributionId, ReceiptNumber, Vendor, FileName, OriginalFileName,
     ContentType, StoragePath, Amount, IssuedOn, IsVerified, UploadedByUserId, CreatedAtUtc)
VALUES
    (@ExpenseId, NULL, @ReceiptNumber, @Vendor, @FileName, @OriginalFileName,
     'image/jpeg', @StoragePath, @Amount, @IssuedOn, @IsVerified, @UploaderId, @CreatedAtUtc);";
        await connection.ExecuteAsync(sql, new
        {
            ExpenseId = expenseId,
            ReceiptNumber = receiptNumber,
            Vendor = vendor,
            FileName = fileName,
            OriginalFileName = originalFileName,
            StoragePath = "assets/images/" + fileName,
            Amount = amount,
            IssuedOn = new DateTime(2025, 10, 24),
            IsVerified = verified,
            UploaderId = uploaderId,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
