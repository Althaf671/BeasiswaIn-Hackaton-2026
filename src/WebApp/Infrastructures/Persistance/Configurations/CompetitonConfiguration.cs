using BeasiswaIn.Web.Common.Constants;
using BeasiswaIn.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Configurations;

internal sealed class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    public void Configure(EntityTypeBuilder<Competition> builder)
    {
        builder.ToTable("competitions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("competition_id")
            .HasMaxLength(IdentityConstant.UlidAbsoluteLength)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Theme)
            .HasColumnName("theme")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.TotalPrize)
            .HasColumnName("total_prize")
            .HasPrecision(15, 2)
            .IsRequired();

        builder.Property(x => x.ContentImageUrl)
            .HasColumnName("content_image_url")
            .HasMaxLength(2056)
            .IsRequired();

        builder.Property(x => x.ContentImagePublicId)
            .HasColumnName("content_image_public_id")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.GuideUrl)
            .HasColumnName("guide_url")
            .HasMaxLength(2056)
            .IsRequired();

        builder.Property(x => x.RegistrationUrl)
            .HasColumnName("registration_url")
            .HasMaxLength(2056)
            .IsRequired();

        builder.Property(x => x.Requirement)
            .HasColumnName("requirement")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(x => x.ContactPerson)
            .HasColumnName("contact_person")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(x => x.IsEligible)
            .HasColumnName("is_eligible")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.RegistrationStartAt)
            .HasColumnName("registration_start_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.RegistrationEndAt)
            .HasColumnName("registration_end_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.IsBookmarked)
            .HasColumnName("is_bookmarked")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.AiRecommendationOpenCount)
            .HasColumnName("ai_recommendation_open_count")
            .IsRequired();

        builder.Property(x => x.UserOpenCount)
            .HasColumnName("user_open_count")
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
    }
}
