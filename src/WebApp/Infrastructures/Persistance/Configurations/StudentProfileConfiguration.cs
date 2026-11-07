using BeasiswaIn.Web.Common.Constants;
using BeasiswaIn.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Configurations;

internal sealed class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("student_profile");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("student_profile_id")
            .HasMaxLength(IdentityConstant.UlidAbsoluteLength)
            .ValueGeneratedNever();

        builder.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Institution)
            .HasColumnName("institution")
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(x => x.FinalScore)
            .HasColumnName("final_score")
            .HasPrecision(15, 2)
            .IsRequired(false);

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(600)
            .IsRequired();

        builder.Property(x => x.Points)
            .HasColumnName("points")
            .IsRequired();

        builder.Property(x => x.AppUserId)
            .HasColumnName("identity_user_id")
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

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.AppUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}