using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inviter.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizerToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizerEmail",
                table: "Events",
                type: "TEXT",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerName",
                table: "Events",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizerEmail",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "OrganizerName",
                table: "Events");
        }
    }
}
