using ATS.Domain.Aggregates.Invitations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATS.Infrastructure.Persistence.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.SecureToken)
            .IsRequired()
            .HasMaxLength(256);

        // Can't easily put unique active invitation constraint purely in DB if status changes, 
        // but we can index email for fast lookups. The Application layer will enforce uniqueness.
        builder.HasIndex(i => new { i.CompanyId, i.Email });
        
        builder.HasIndex(i => i.SecureToken).IsUnique();
    }
}
