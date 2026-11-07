using BeasiswaIn.Web.Common.Constants;
using BeasiswaIn.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Configurations;

internal sealed class UserScholarshipPreferencesConfiguration : IEntityTypeConfiguration<UserScholarshipPreferences>
{
    public void Configure(EntityTypeBuilder<UserScholarshipPreferences> builder)
    {
        builder.ToTable("student_scholarship_preferences");
        builder.HasKey(x => new { x.StudentId, x.ScholarshipId });

        builder.Property(x => x.StudentId)
            .HasColumnName("student_id")
            .HasMaxLength(IdentityConstant.UlidAbsoluteLength)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.ScholarshipId)
            .HasColumnName("scholarship_id")
            .HasMaxLength(IdentityConstant.UlidAbsoluteLength)
            .ValueGeneratedNever()
            .IsRequired();

        builder.HasOne<StudentProfile>()
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Scholarship>()
            .WithMany()
            .HasForeignKey(x => x.ScholarshipId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
