using BeasiswaIn.Web.Common.Constants;
using BeasiswaIn.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Configurations;

internal sealed class UserCompetitionPreferencesConfiguration : IEntityTypeConfiguration<UserCompetitionPreferences>
{
    public void Configure(EntityTypeBuilder<UserCompetitionPreferences> builder)
    {
        builder.ToTable("user_competition_preferences");
        builder.HasKey(x => new { x.UserId, x.CategoryId });

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(IdentityConstant.UlidAbsoluteLength)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.CategoryId)
            .HasColumnName("category_id")
            .HasMaxLength(IdentityConstant.UlidAbsoluteLength)
            .ValueGeneratedNever()
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CompetitionCategory>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
