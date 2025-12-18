using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocationVoiture.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationToClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Clients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                table: "Clients");
        }
    }
}
