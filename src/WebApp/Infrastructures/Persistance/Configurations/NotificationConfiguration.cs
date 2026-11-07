using BeasiswaIn.Web.Common.Constants;
using BeasiswaIn.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeasiswaIn.Web.Infrastructures.Persistance.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notification");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.AppUserId, x.Id })
            .IsDescending(false, true);

        builder.Property(x => x.Id)
            .HasColumnName("notification_id")
            .HasMaxLength(IdentityConstant.UlidAbsoluteLength)
            .ValueGeneratedNever();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnName("content")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(75)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(75)
            .IsRequired();

        builder.Property(x => x.AppUserId)
            .HasColumnName("recepient_user_id")
            .HasMaxLength(IdentityConstant.UlidAbsoluteLength)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.AppUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}