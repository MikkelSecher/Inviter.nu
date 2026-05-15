using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inviter.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEventOptionsAndRsvpContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Rsvps",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Rsvps",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowMaybe",
                table: "Events",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ContactRequirement",
                table: "Events",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RsvpDeadline",
                table: "Events",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "AllowMaybe",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ContactRequirement",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RsvpDeadline",
                table: "Events");
        }
    }
}
