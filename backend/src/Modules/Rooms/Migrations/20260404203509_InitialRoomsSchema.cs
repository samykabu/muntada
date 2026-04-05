using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Muntada.Rooms.Migrations
{
    /// <inheritdoc />
    public partial class InitialRoomsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rooms");

            migrationBuilder.CreateTable(
                name: "Recordings",
                schema: "rooms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoomOccurrenceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    S3Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    DurationSeconds = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Visibility = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LiveKitEgressId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recordings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomInvites",
                schema: "rooms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoomOccurrenceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvitedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    InvitedUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InviteToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InviteType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    InvitedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomInvites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomOccurrences",
                schema: "rooms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoomSeriesId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OrganizerTimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LiveStartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LiveEndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ModeratorUserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModeratorAssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModeratorDisconnectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    AllowGuestAccess = table.Column<bool>(type: "bit", nullable: false),
                    AllowRecording = table.Column<bool>(type: "bit", nullable: false),
                    AllowTranscription = table.Column<bool>(type: "bit", nullable: false),
                    DefaultTranscriptionLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AutoStartRecording = table.Column<bool>(type: "bit", nullable: false),
                    GracePeriodSeconds = table.Column<int>(type: "int", nullable: false),
                    GraceStartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomOccurrences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomParticipantStates",
                schema: "rooms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoomOccurrenceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LeftAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AudioState = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VideoState = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LiveKitParticipantId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomParticipantStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomSeries",
                schema: "rooms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TemplateId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecurrenceRule = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OrganizerTimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomSeries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomTemplates",
                schema: "rooms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    AllowGuestAccess = table.Column<bool>(type: "bit", nullable: false),
                    AllowRecording = table.Column<bool>(type: "bit", nullable: false),
                    AllowTranscription = table.Column<bool>(type: "bit", nullable: false),
                    DefaultTranscriptionLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AutoStartRecording = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transcript",
                schema: "rooms",
                columns: table => new
                {
                    RecordingId = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    S3Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TextS3Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transcript", x => new { x.RecordingId, x.Id });
                    table.ForeignKey(
                        name: "FK_Transcript_Recordings_RecordingId",
                        column: x => x.RecordingId,
                        principalSchema: "rooms",
                        principalTable: "Recordings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_RoomOccurrenceId",
                schema: "rooms",
                table: "Recordings",
                column: "RoomOccurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_TenantId",
                schema: "rooms",
                table: "Recordings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomInvites_InviteToken",
                schema: "rooms",
                table: "RoomInvites",
                column: "InviteToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomInvites_RoomOccurrenceId_Status",
                schema: "rooms",
                table: "RoomInvites",
                columns: new[] { "RoomOccurrenceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomOccurrences_RoomSeriesId",
                schema: "rooms",
                table: "RoomOccurrences",
                column: "RoomSeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomOccurrences_TenantId_ScheduledAt",
                schema: "rooms",
                table: "RoomOccurrences",
                columns: new[] { "TenantId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomOccurrences_TenantId_Status",
                schema: "rooms",
                table: "RoomOccurrences",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomParticipantStates_RoomOccurrenceId_LeftAt",
                schema: "rooms",
                table: "RoomParticipantStates",
                columns: new[] { "RoomOccurrenceId", "LeftAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomSeries_TenantId_Status",
                schema: "rooms",
                table: "RoomSeries",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomTemplates_TenantId_Name",
                schema: "rooms",
                table: "RoomTemplates",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomInvites",
                schema: "rooms");

            migrationBuilder.DropTable(
                name: "RoomOccurrences",
                schema: "rooms");

            migrationBuilder.DropTable(
                name: "RoomParticipantStates",
                schema: "rooms");

            migrationBuilder.DropTable(
                name: "RoomSeries",
                schema: "rooms");

            migrationBuilder.DropTable(
                name: "RoomTemplates",
                schema: "rooms");

            migrationBuilder.DropTable(
                name: "Transcript",
                schema: "rooms");

            migrationBuilder.DropTable(
                name: "Recordings",
                schema: "rooms");
        }
    }
}
