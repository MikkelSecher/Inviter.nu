using System;
using Inviter.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inviter.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260712120000_AddPersonalInviteLinksAndRsvpLinks")]
    public partial class AddPersonalInviteLinksAndRsvpLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                PRAGMA foreign_keys=OFF;

                DROP INDEX IF EXISTS "IX_Invitees_EventId_Email";
                ALTER TABLE "Invitees" RENAME TO "Invitees_old";

                CREATE TABLE "Invitees" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_Invitees" PRIMARY KEY,
                    "EventId" TEXT NOT NULL,
                    "PersonalInviteToken" TEXT NOT NULL,
                    "Email" TEXT NULL,
                    "Name" TEXT NULL,
                    "AddedAt" TEXT NOT NULL,
                    "LastSentAt" TEXT NULL,
                    "SendCount" INTEGER NOT NULL,
                    CONSTRAINT "FK_Invitees_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE CASCADE
                );

                INSERT INTO "Invitees" ("Id", "EventId", "PersonalInviteToken", "Email", "Name", "AddedAt", "LastSentAt", "SendCount")
                SELECT "Id", "EventId", lower(hex(randomblob(12))), "Email", "Name", "AddedAt", "LastSentAt", "SendCount"
                FROM "Invitees_old";

                DROP TABLE "Invitees_old";

                CREATE UNIQUE INDEX "IX_Invitees_EventId_Email" ON "Invitees" ("EventId", "Email");
                CREATE UNIQUE INDEX "IX_Invitees_EventId_PersonalInviteToken" ON "Invitees" ("EventId", "PersonalInviteToken");

                DROP INDEX IF EXISTS "IX_Rsvps_EventId";
                ALTER TABLE "Rsvps" RENAME TO "Rsvps_old";

                CREATE TABLE "Rsvps" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_Rsvps" PRIMARY KEY,
                    "EventId" TEXT NOT NULL,
                    "InviteeId" TEXT NULL,
                    "GuestName" TEXT NOT NULL,
                    "Status" INTEGER NOT NULL,
                    "Comment" TEXT NULL,
                    "Email" TEXT NULL,
                    "Phone" TEXT NULL,
                    "SubmittedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_Rsvps_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_Rsvps_Invitees_InviteeId" FOREIGN KEY ("InviteeId") REFERENCES "Invitees" ("Id") ON DELETE SET NULL
                );

                INSERT INTO "Rsvps" ("Id", "EventId", "InviteeId", "GuestName", "Status", "Comment", "Email", "Phone", "SubmittedAt")
                SELECT "Id", "EventId", NULL, "GuestName", "Status", "Comment", "Email", "Phone", "SubmittedAt"
                FROM "Rsvps_old";

                DROP TABLE "Rsvps_old";

                CREATE INDEX "IX_Rsvps_EventId" ON "Rsvps" ("EventId");
                CREATE INDEX "IX_Rsvps_InviteeId" ON "Rsvps" ("InviteeId");

                PRAGMA foreign_keys=ON;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                PRAGMA foreign_keys=OFF;

                DROP INDEX IF EXISTS "IX_Rsvps_EventId";
                DROP INDEX IF EXISTS "IX_Rsvps_InviteeId";
                ALTER TABLE "Rsvps" RENAME TO "Rsvps_old";

                CREATE TABLE "Rsvps" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_Rsvps" PRIMARY KEY,
                    "EventId" TEXT NOT NULL,
                    "GuestName" TEXT NOT NULL,
                    "Status" INTEGER NOT NULL,
                    "Comment" TEXT NULL,
                    "Email" TEXT NULL,
                    "Phone" TEXT NULL,
                    "SubmittedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_Rsvps_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE CASCADE
                );

                INSERT INTO "Rsvps" ("Id", "EventId", "GuestName", "Status", "Comment", "Email", "Phone", "SubmittedAt")
                SELECT "Id", "EventId", "GuestName", "Status", "Comment", "Email", "Phone", "SubmittedAt"
                FROM "Rsvps_old";

                DROP TABLE "Rsvps_old";
                CREATE INDEX "IX_Rsvps_EventId" ON "Rsvps" ("EventId");

                DROP INDEX IF EXISTS "IX_Invitees_EventId_Email";
                DROP INDEX IF EXISTS "IX_Invitees_EventId_PersonalInviteToken";
                ALTER TABLE "Invitees" RENAME TO "Invitees_old";

                CREATE TABLE "Invitees" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_Invitees" PRIMARY KEY,
                    "EventId" TEXT NOT NULL,
                    "Email" TEXT NOT NULL,
                    "Name" TEXT NULL,
                    "AddedAt" TEXT NOT NULL,
                    "LastSentAt" TEXT NULL,
                    "SendCount" INTEGER NOT NULL,
                    CONSTRAINT "FK_Invitees_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE CASCADE
                );

                INSERT INTO "Invitees" ("Id", "EventId", "Email", "Name", "AddedAt", "LastSentAt", "SendCount")
                SELECT "Id", "EventId", COALESCE("Email", "PersonalInviteToken" || '@invitee.invalid'), "Name", "AddedAt", "LastSentAt", "SendCount"
                FROM "Invitees_old";

                DROP TABLE "Invitees_old";
                CREATE UNIQUE INDEX "IX_Invitees_EventId_Email" ON "Invitees" ("EventId", "Email");

                PRAGMA foreign_keys=ON;
                """);
        }
    }
}
