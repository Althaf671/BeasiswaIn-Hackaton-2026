using BeasiswaIn.Web.Common.Constants;
using BeasiswaIn.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Configurations;

internal sealed class SubmittedScholarshipConfiguration : IEntityTypeConfiguration<SubmittedScholarship>
{
    public void Configure(EntityTypeBuilder<SubmittedScholarship> builder)
    {
        builder.ToTable("submitted_scholarship");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("submitted_scholarship_id")
            .HasMaxLength(IdentityConstant.UlidAbsoluteLength)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.ScholarshipSourceUrl)
            .HasColumnName("scholarship_source_url")
            .HasMaxLength(2100)
            .IsRequired(false);

        builder.Property(x => x.ScholarshipContentUrl)
            .HasColumnName("scholarship_content_url")
            .HasMaxLength(2100)
            .IsRequired(false);

        builder.Property(x => x.ScholarshipContentPublicId)
            .HasColumnName("scholarship_content_public_id")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.StudentId)
            .HasColumnName("student_profile_id")
            .HasMaxLength(IdentityConstant.UlidAbsoluteLength)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(x => x.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(x => x.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne<StudentProfile>()
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}