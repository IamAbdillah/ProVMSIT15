using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ProVMSIT15.Models;

namespace ProVMSIT15.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<VendorItem> VendorItems { get; set; }
    public DbSet<PurchaseRequisition> PurchaseRequisitions { get; set; }
    public DbSet<SupplierEvaluation> SupplierEvaluations { get; set; }
    public DbSet<InAppNotification> InAppNotifications { get; set; }
    public DbSet<DepartmentBudget> DepartmentBudgets { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<ContractItem> ContractItems { get; set; }
    public DbSet<FinancialAuditTrail> FinancialAuditTrails { get; set; }
    public DbSet<SLAMilestoneLog> SLAMilestoneLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.UserRole).HasConversion<string>();
            e.HasQueryFilter(u => !u.IsArchived);
        });

        modelBuilder.Entity<Vendor>(e =>
        {
            e.HasIndex(v => v.TaxID).IsUnique();
            e.Property(v => v.OperationalStatus).HasConversion<string>();
            e.HasOne(v => v.LinkedUser)
             .WithMany()
             .HasForeignKey(v => v.LinkedUserID)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VendorItem>(e =>
        {
            e.Property(vi => vi.Category).HasConversion<string>();
            e.HasOne(vi => vi.Vendor)
             .WithMany(v => v.Items)
             .HasForeignKey(vi => vi.VendorID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseRequisition>(e =>
        {
            e.Property(pr => pr.WorkflowStatus).HasConversion<string>();
            e.HasOne(pr => pr.Requester)
             .WithMany(u => u.Requisitions)
             .HasForeignKey(pr => pr.RequesterID)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(pr => pr.Item)
             .WithMany(vi => vi.Requisitions)
             .HasForeignKey(pr => pr.ItemID)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierEvaluation>(e =>
        {
            e.HasOne(se => se.Requisition)
             .WithOne(pr => pr.Evaluation)
             .HasForeignKey<SupplierEvaluation>(se => se.RequisitionID)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(se => se.Vendor)
             .WithMany(v => v.Evaluations)
             .HasForeignKey(se => se.VendorID)
             .OnDelete(DeleteBehavior.SetNull);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_DeliverySpeed", "`DeliverySpeedStars` BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_ItemCondition", "`ItemConditionStars` BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_Communication", "`CommunicationStars` BETWEEN 1 AND 5");
            });
        });

        modelBuilder.Entity<InAppNotification>(e =>
        {
            e.HasOne(n => n.TargetUser)
             .WithMany(u => u.Notifications)
             .HasForeignKey(n => n.TargetUserID)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DepartmentBudget>(e =>
        {
            e.HasIndex(db => new { db.DepartmentCode, db.FiscalYear }).IsUnique();
        });

        modelBuilder.Entity<Contract>(e =>
        {
            e.Property(c => c.Status).HasConversion<string>();
            e.Property(c => c.TotalValue).HasColumnType("decimal(15,2)");
            e.Property(c => c.DiscountPercent).HasColumnType("decimal(5,2)");
            e.HasOne(c => c.Vendor)
             .WithMany()
             .HasForeignKey(c => c.VendorID)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FinancialAuditTrail>(e =>
        {
            e.HasKey(a => a.AuditID);
            e.Property(a => a.TransactionType).HasMaxLength(50);
            e.Property(a => a.MachineIPAddress).HasMaxLength(45);
            e.Property(a => a.JWTSignatureHash).HasMaxLength(255);
            e.Property(a => a.SystemTimestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasOne(a => a.Actor)
             .WithMany()
             .HasForeignKey(a => a.UserID)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SLAMilestoneLog>(e =>
        {
            e.HasKey(s => s.LogID);
            e.Property(s => s.WorkflowType).HasConversion<string>();
            e.Property(s => s.SLABreachStatus).HasConversion<string>();
            e.Property(s => s.DurationHours).HasColumnType("decimal(6,2)");
            e.Property(s => s.StartTimestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(s => s.UpdatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<ContractItem>(e =>
        {
            e.Property(ci => ci.NegotiatedUnitPrice).HasColumnType("decimal(12,2)");
            e.HasOne(ci => ci.Contract)
             .WithMany(c => c.Items)
             .HasForeignKey(ci => ci.ContractID)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ci => ci.VendorItem)
             .WithMany()
             .HasForeignKey(ci => ci.VendorItemID)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ── CFO IMMUTABILITY GUARD ──────────────────────────────────────────
    // Blocks all Delete operations on immutable financial ledger tables.
    public override int SaveChanges()
    {
        EnforceImmutability();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnforceImmutability();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void EnforceImmutability()
    {
        var forbidden = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Deleted &&
                        (e.Entity is FinancialAuditTrail ||
                         e.Entity is PurchaseRequisition ||
                         e.Entity is Contract ||
                         e.Entity is ContractItem))
            .ToList();

        if (forbidden.Count > 0)
        {
            var typeName = forbidden.First().Entity.GetType().Name;
            throw new InvalidOperationException(
                $"CFO IMMUTABILITY VIOLATION: Deletion of '{typeName}' records is permanently prohibited. " +
                "Financial ledger entries are append-only.");
        }
    }
}
