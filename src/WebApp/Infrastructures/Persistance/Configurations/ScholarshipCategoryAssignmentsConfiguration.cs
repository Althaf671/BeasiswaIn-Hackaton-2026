using BeasiswaIn.Web.Common.Constants;
using BeasiswaIn.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Configurations;

internal sealed class ScholarshipCategoryAssignmentsConfiguration : IEntityTypeConfiguration<ScholarshipCategoryAssignments>
{
    public void Configure(EntityTypeBuilder<ScholarshipCategoryAssignments> builder)
    {
        builder.ToTable("scholarship_category_assignments");
        builder.HasKey(x => new { x.ScholarshipId, x.CategoryId });

        builder.Property(x => x.ScholarshipId)
            .HasColumnName("scholarship_id")
            .HasMaxLength(IdentityConstant.UlidAbsoluteLength)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.CategoryId)
            .HasColumnName("category_id")
            .HasMaxLength(IdentityConstant.UlidAbsoluteLength)
            .ValueGeneratedNever()
            .IsRequired();

        builder.HasOne<Scholarship>()
            .WithMany()
            .HasForeignKey(x => x.ScholarshipId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<ScholarshipCategory>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);
    }
}
