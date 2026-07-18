using ATS.Domain.Aggregates.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATS.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(j => j.Description)
            .IsRequired();

        builder.Property(j => j.Location)
            .IsRequired()
            .HasMaxLength(200);

        builder.OwnsOne(j => j.SalaryRange, salary =>
        {
            salary.Property(s => s.Min)
                .HasColumnName("SalaryMin")
                .HasColumnType("decimal(18,2)");
                
            salary.Property(s => s.Max)
                .HasColumnName("SalaryMax")
                .HasColumnType("decimal(18,2)");
                
            salary.Property(s => s.Currency)
                .HasColumnName("SalaryCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Ensure navigation to Company exists conceptually
        builder.HasOne<ATS.Domain.Aggregates.Companies.Company>()
            .WithMany()
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ensure navigation to Department exists conceptually
        builder.HasOne<ATS.Domain.Aggregates.Departments.Department>()
            .WithMany()
            .HasForeignKey(j => j.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasQueryFilter(j => true); // In case of future soft delete
    }
}
