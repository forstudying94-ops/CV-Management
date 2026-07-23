using ItransitionCourseProject.Models;
using Microsoft.EntityFrameworkCore;
using Attribute = ItransitionCourseProject.Models.Attribute;

namespace ItransitionCourseProject.DataBase;

public class DatabaseContext : DbContext {
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) {}

    public DbSet<User> Users => Set<User>();
    public DbSet<UserExternalLogin> UserExternalLogins => Set<UserExternalLogin>();
    public DbSet<ProfileCandidate> ProfileCandidates => Set<ProfileCandidate>();
    public DbSet<Attribute> Attributes => Set<Attribute>();
    public DbSet<ProfileAttributeBinding> ProfileAttributeBindings => Set<ProfileAttributeBinding>();
    public DbSet<AttributeValue> AttributeValues => Set<AttributeValue>();
    public DbSet<TechnologyTag> TechnologyTags => Set<TechnologyTag>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionAttributeBinding> PositionAttributeBindings => Set<PositionAttributeBinding>();
    public DbSet<PositionProjectTag> PositionProjectTags => Set<PositionProjectTag>();
    public DbSet<CV> Cvs => Set<CV>();
    public DbSet<CvLike> CvLikes => Set<CvLike>();
    public DbSet<Discussion> Discussions => Set<Discussion>();

    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);

        ConfigureUsers(builder);
        ConfigureProfiles(builder);
        ConfigureAttributes(builder);
        ConfigureTechnologyTags(builder);
        ConfigurePositions(builder);
        ConfigureCvs(builder);
        ConfigureDiscussions(builder);
    }

    private static void ConfigureUsers(ModelBuilder builder) {
        builder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.UserId);

            entity.Property(user => user.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(512);

            entity.Property(user => user.Company)
                .HasMaxLength(200);

            entity.Property(user => user.ProfilePicUrl)
                .HasMaxLength(2_000);

            entity.Property(user => user.ProfilePicPublicId)
                .HasMaxLength(500);

            entity.Property(user => user.Role)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(user => user.Theme)
                .HasConversion<string>()
                .HasMaxLength(10)
                .HasDefaultValue(UserTheme.Light);

            entity.Property(user => user.IsBlocked)
                .HasDefaultValue(false);

            entity.Property(user => user.Version)
                .HasDefaultValue(1)
                .IsConcurrencyToken();

            entity.Property(user => user.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(user => user.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(user => user.ProfileForUser)
                .WithOne(profile => profile.UserForProfile)
                .HasForeignKey<ProfileCandidate>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserExternalLogin>(entity =>
        {
            entity.HasKey(login => new { login.Provider, login.Subject });

            entity.Property(login => login.Provider)
                .HasMaxLength(50);

            entity.Property(login => login.Subject)
                .HasMaxLength(200);

            entity.HasIndex(login => new { login.UserId, login.Provider })
                .IsUnique();

            entity.HasOne(login => login.UserForExternalLogin)
                .WithMany(user => user.ExternalLoginForUser)
                .HasForeignKey(login => login.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProfiles(ModelBuilder builder) {
        builder.Entity<ProfileCandidate>(entity =>
        {
            entity.HasKey(profile => profile.ProfileCandidateId);

            entity.Property(profile => profile.Location)
                .HasMaxLength(200);

            entity.Property(profile => profile.Version)
                .HasDefaultValue(1)
                .IsConcurrencyToken();

            entity.Property(profile => profile.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(profile => profile.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(profile => profile.UserId)
                .IsUnique();
        });
    }

    private static void ConfigureAttributes(ModelBuilder builder) {
        builder.Entity<Attribute>(entity =>
        {
            entity.HasKey(attribute => attribute.AttributeId);

            entity.Property(attribute => attribute.AttributeName)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(attribute => attribute.AttributeName)
                .IsUnique();

            entity.Property(attribute => attribute.Category)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(attribute => attribute.IsSystem)
                .HasDefaultValue(false);

            entity.Property(attribute => attribute.IsDeleted)
                .HasDefaultValue(false);

            entity.Property(attribute => attribute.Version)
                .HasDefaultValue(1)
                .IsConcurrencyToken();

            entity.Property(attribute => attribute.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(attribute => attribute.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<ProfileAttributeBinding>(entity =>
        {
            entity.HasKey(binding => new { binding.ProfileCandidateId, binding.AttributeId });

            entity.HasOne(binding => binding.ProfileForBinding)
                .WithMany(profile => profile.BindingForProfile)
                .HasForeignKey(binding => binding.ProfileCandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(binding => binding.AttributeForBinding)
                .WithMany(attribute => attribute.ProfileBindingForAttribute)
                .HasForeignKey(binding => binding.AttributeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AttributeValue>(entity =>
        {
            entity.HasKey(value => value.AttributeValueId);

            entity.Property(value => value.Value)
                .HasColumnType("text");

            entity.Property(value => value.Version)
                .HasDefaultValue(1)
                .IsConcurrencyToken();

            entity.Property(value => value.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(value => new { value.ProfileCandidateId, value.AttributeId })
                .IsUnique();

            entity.HasOne(value => value.BindingForValue)
                .WithOne(binding => binding.ValueForBinding)
                .HasForeignKey<AttributeValue>(value => new
                {
                    value.ProfileCandidateId,
                    value.AttributeId
                })
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTechnologyTags(ModelBuilder builder) {
        builder.Entity<TechnologyTag>(entity =>
        {
            entity.HasKey(tag => tag.TechnologyTagId);

            entity.Property(tag => tag.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(tag => tag.Name)
                .IsUnique();
        });
    }

    private static void ConfigurePositions(ModelBuilder builder) {
        builder.Entity<Position>(entity =>
        {
            entity.HasKey(position => position.PositionId);

            entity.Property(position => position.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(position => position.ShortDescription)
                .HasMaxLength(1_000);

            entity.Property(position => position.IsDeleted)
                .HasDefaultValue(false);

            entity.Property(position => position.Version)
                .HasDefaultValue(1)
                .IsConcurrencyToken();

            entity.Property(position => position.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(position => position.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

        });

        builder.Entity<PositionAttributeBinding>(entity =>
        {
            entity.HasKey(binding => new { binding.PositionId, binding.AttributeId });

            entity.HasOne(binding => binding.PositionForBinding)
                .WithMany(position => position.AttributeBindingForPosition)
                .HasForeignKey(binding => binding.PositionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(binding => binding.AttributeForBinding)
                .WithMany(attribute => attribute.PositionBindingForAttribute)
                .HasForeignKey(binding => binding.AttributeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PositionProjectTag>(entity =>
        {
            entity.HasKey(binding => new { binding.PositionId, binding.TechnologyTagId });

            entity.HasOne(binding => binding.PositionForBinding)
                .WithMany(position => position.ProjectTagBindingForPosition)
                .HasForeignKey(binding => binding.PositionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(binding => binding.TechnologyTagForBinding)
                .WithMany(tag => tag.PositionBindingForTechnologyTag)
                .HasForeignKey(binding => binding.TechnologyTagId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCvs(ModelBuilder builder) {
        builder.Entity<CV>(entity =>
        {
            entity.HasKey(cv => cv.CvId);

            entity.Property(cv => cv.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(CvStatus.Draft);

            entity.Property(cv => cv.Version)
                .HasDefaultValue(1)
                .IsConcurrencyToken();

            entity.Property(cv => cv.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(cv => cv.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(cv => new { cv.ProfileCandidateId, cv.PositionId })
                .IsUnique();

            entity.HasIndex(cv => new { cv.Status, cv.PublishedAt });

            entity.HasOne(cv => cv.ProfileForCv)
                .WithMany(profile => profile.CvForProfile)
                .HasForeignKey(cv => cv.ProfileCandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cv => cv.PositionForCv)
                .WithMany(position => position.CvForPosition)
                .HasForeignKey(cv => cv.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CvLike>(entity =>
        {
            entity.HasKey(like => new { like.CvId, like.RecruiterId });

            entity.Property(like => like.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(like => like.CvForLike)
                .WithMany(cv => cv.LikeForCv)
                .HasForeignKey(like => like.CvId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(like => like.RecruiterForLike)
                .WithMany(user => user.CvLikeForUser)
                .HasForeignKey(like => like.RecruiterId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDiscussions(ModelBuilder builder) {
        builder.Entity<Discussion>(entity =>
        {
            entity.HasKey(discussion => discussion.DiscussionId);

            entity.Property(discussion => discussion.AuthorDisplayName)
                .HasMaxLength(201)
                .IsRequired();

            entity.Property(discussion => discussion.ContentMarkdown)
                .HasColumnType("text")
                .IsRequired();

            entity.Property(discussion => discussion.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(discussion => new { discussion.PositionId, discussion.CreatedAt });

            entity.HasOne(discussion => discussion.PositionForDiscussion)
                .WithMany(position => position.DiscussionForPosition)
                .HasForeignKey(discussion => discussion.PositionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(discussion => discussion.AuthorForDiscussion)
                .WithMany(user => user.DiscussionForUser)
                .HasForeignKey(discussion => discussion.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
