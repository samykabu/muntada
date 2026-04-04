using Microsoft.EntityFrameworkCore;
using Muntada.Rooms.Domain.Invite;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Participant;
using Muntada.Rooms.Domain.Recording;
using Muntada.Rooms.Domain.Series;
using Muntada.Rooms.Domain.Template;

namespace Muntada.Rooms.Infrastructure;

/// <summary>
/// Entity Framework Core DbContext for the Rooms module.
/// All tables are created under the <c>[rooms]</c> SQL Server schema
/// per Constitution I (Modular Monolith Discipline).
/// </summary>
public class RoomsDbContext : DbContext
{
    /// <summary>The SQL Server schema name for all Rooms module tables.</summary>
    public const string SchemaName = "rooms";

    /// <summary>
    /// Initializes a new instance of the <see cref="RoomsDbContext"/> class.
    /// </summary>
    public RoomsDbContext(DbContextOptions<RoomsDbContext> options) : base(options) { }

    /// <summary>Gets or sets the room templates.</summary>
    public DbSet<RoomTemplate> RoomTemplates => Set<RoomTemplate>();

    /// <summary>Gets or sets the room series.</summary>
    public DbSet<RoomSeries> RoomSeries => Set<RoomSeries>();

    /// <summary>Gets or sets the room occurrences.</summary>
    public DbSet<RoomOccurrence> RoomOccurrences => Set<RoomOccurrence>();

    /// <summary>Gets or sets the room invites.</summary>
    public DbSet<RoomInvite> RoomInvites => Set<RoomInvite>();

    /// <summary>Gets or sets the room participant states.</summary>
    public DbSet<RoomParticipantState> RoomParticipantStates => Set<RoomParticipantState>();

    /// <summary>Gets or sets the recordings.</summary>
    public DbSet<Recording> Recordings => Set<Recording>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        ConfigureRoomTemplate(modelBuilder);
        ConfigureRoomSeries(modelBuilder);
        ConfigureRoomOccurrence(modelBuilder);
        ConfigureRoomInvite(modelBuilder);
        ConfigureRoomParticipantState(modelBuilder);
        ConfigureRecording(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureRoomTemplate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoomTemplate>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id)
                .HasConversion(v => v.Value, v => new RoomTemplateId(v))
                .HasMaxLength(50);
            entity.Property(t => t.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Description).HasMaxLength(500);
            entity.Property(t => t.CreatedBy).IsRequired().HasMaxLength(50);
            entity.Property(t => t.Version).IsConcurrencyToken();

            entity.HasIndex(t => new { t.TenantId, t.Name }).IsUnique();

            entity.OwnsOne(t => t.Settings, settings =>
            {
                settings.Property(s => s.MaxParticipants).HasColumnName("MaxParticipants");
                settings.Property(s => s.AllowGuestAccess).HasColumnName("AllowGuestAccess");
                settings.Property(s => s.AllowRecording).HasColumnName("AllowRecording");
                settings.Property(s => s.AllowTranscription).HasColumnName("AllowTranscription");
                settings.Property(s => s.DefaultTranscriptionLanguage).HasColumnName("DefaultTranscriptionLanguage").HasMaxLength(10);
                settings.Property(s => s.AutoStartRecording).HasColumnName("AutoStartRecording");
            });
        });
    }

    private static void ConfigureRoomSeries(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoomSeries>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id)
                .HasConversion(v => v.Value, v => new RoomSeriesId(v))
                .HasMaxLength(50);
            entity.Property(s => s.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(s => s.TemplateId)
                .HasConversion(v => v.Value, v => new RoomTemplateId(v))
                .IsRequired().HasMaxLength(50);
            entity.Property(s => s.Title).IsRequired().HasMaxLength(200);
            entity.Property(s => s.RecurrenceRule).IsRequired().HasMaxLength(500);
            entity.Property(s => s.OrganizerTimeZoneId).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(s => s.CreatedBy).IsRequired().HasMaxLength(50);
            entity.Property(s => s.Version).IsConcurrencyToken();

            entity.HasIndex(s => new { s.TenantId, s.Status });
        });
    }

    private static void ConfigureRoomOccurrence(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoomOccurrence>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Id)
                .HasConversion(v => v.Value, v => new RoomOccurrenceId(v))
                .HasMaxLength(50);
            entity.Property(o => o.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(o => o.RoomSeriesId)
                .HasConversion(v => v == null ? null : v.Value, v => v == null ? null : new RoomSeriesId(v))
                .HasMaxLength(50);
            entity.Property(o => o.Title).IsRequired().HasMaxLength(200);
            entity.Property(o => o.OrganizerTimeZoneId).IsRequired().HasMaxLength(100);
            entity.Property(o => o.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(o => o.CreatedBy).IsRequired().HasMaxLength(50);
            entity.Property(o => o.Version).IsConcurrencyToken();

            entity.HasIndex(o => new { o.TenantId, o.Status });
            entity.HasIndex(o => new { o.TenantId, o.ScheduledAt });
            entity.HasIndex(o => o.RoomSeriesId);

            entity.OwnsOne(o => o.ModeratorAssignment, mod =>
            {
                mod.Property(m => m.UserId).HasColumnName("ModeratorUserId").HasMaxLength(50);
                mod.Property(m => m.AssignedAt).HasColumnName("ModeratorAssignedAt");
                mod.Property(m => m.DisconnectedAt).HasColumnName("ModeratorDisconnectedAt");
            });

            entity.OwnsOne(o => o.Settings, settings =>
            {
                settings.Property(s => s.MaxParticipants).HasColumnName("MaxParticipants");
                settings.Property(s => s.AllowGuestAccess).HasColumnName("AllowGuestAccess");
                settings.Property(s => s.AllowRecording).HasColumnName("AllowRecording");
                settings.Property(s => s.AllowTranscription).HasColumnName("AllowTranscription");
                settings.Property(s => s.DefaultTranscriptionLanguage).HasColumnName("DefaultTranscriptionLanguage").HasMaxLength(10);
                settings.Property(s => s.AutoStartRecording).HasColumnName("AutoStartRecording");
            });

            entity.Ignore(o => o.Invites);
            entity.Ignore(o => o.Participants);
        });
    }

    private static void ConfigureRoomInvite(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoomInvite>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Id)
                .HasConversion(v => v.Value, v => new RoomInviteId(v))
                .HasMaxLength(50);
            entity.Property(i => i.RoomOccurrenceId)
                .HasConversion(v => v.Value, v => new RoomOccurrenceId(v))
                .IsRequired().HasMaxLength(50);
            entity.Property(i => i.InvitedEmail).HasMaxLength(256);
            entity.Property(i => i.InvitedUserId).HasMaxLength(50);
            entity.Property(i => i.InviteToken).IsRequired().HasMaxLength(64);
            entity.Property(i => i.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(i => i.InviteType).IsRequired().HasConversion<string>().HasMaxLength(30);
            entity.Property(i => i.InvitedBy).IsRequired().HasMaxLength(50);

            entity.HasIndex(i => i.InviteToken).IsUnique();
            entity.HasIndex(i => new { i.RoomOccurrenceId, i.Status });
        });
    }

    private static void ConfigureRoomParticipantState(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoomParticipantState>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id)
                .HasConversion(v => v.Value, v => new RoomParticipantStateId(v))
                .HasMaxLength(50);
            entity.Property(p => p.RoomOccurrenceId)
                .HasConversion(v => v.Value, v => new RoomOccurrenceId(v))
                .IsRequired().HasMaxLength(50);
            entity.Property(p => p.UserId).HasMaxLength(50);
            entity.Property(p => p.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Role).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.AudioState).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.VideoState).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.LiveKitParticipantId).HasMaxLength(200);

            entity.HasIndex(p => new { p.RoomOccurrenceId, p.LeftAt });
        });
    }

    private static void ConfigureRecording(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recording>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id)
                .HasConversion(v => v.Value, v => new RecordingId(v))
                .HasMaxLength(50);
            entity.Property(r => r.RoomOccurrenceId)
                .HasConversion(v => v.Value, v => new RoomOccurrenceId(v))
                .IsRequired().HasMaxLength(50);
            entity.Property(r => r.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(r => r.S3Path).IsRequired().HasMaxLength(500);
            entity.Property(r => r.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.Visibility).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.LiveKitEgressId).HasMaxLength(200);
            entity.Property(r => r.Version).IsConcurrencyToken();

            entity.HasIndex(r => r.RoomOccurrenceId);
            entity.HasIndex(r => r.TenantId);

            entity.OwnsMany(r => r.Transcripts, transcript =>
            {
                transcript.Property(t => t.Language).IsRequired().HasMaxLength(10);
                transcript.Property(t => t.S3Path).IsRequired().HasMaxLength(500);
                transcript.Property(t => t.TextS3Path).IsRequired().HasMaxLength(500);
                transcript.Property(t => t.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            });
        });
    }
}
