using BeasiswaIn.Web.Common.Constants;
using BeasiswaIn.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Configurations;

internal sealed class CompetitionCategoryAssignmentsConfiguration : IEntityTypeConfiguration<CompetitionCategoryAssignments>
{
    public void Configure(EntityTypeBuilder<CompetitionCategoryAssignments> builder)
    {
        builder.ToTable("competition_category_assignments");
        builder.HasKey(x => new { x.CompetitionId, x.CategoryId });

        builder.Property(x => x.CompetitionId)
            .HasColumnName("competition_id")
            .HasMaxLength(IdentityConstant.UlidAbsoluteLength)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.CategoryId)
            .HasColumnName("category_id")
            .HasMaxLength(IdentityConstant.UlidAbsoluteLength)
            .ValueGeneratedNever()
            .IsRequired();

        builder.HasOne<Competition>()
            .WithMany()
            .HasForeignKey(x => x.CompetitionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<CompetitionCategory>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);
    }
}
