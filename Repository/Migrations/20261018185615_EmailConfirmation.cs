using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class EmailConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmTokenExpires",
                table: "Accounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmTokenHash",
                table: "Accounts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "Accounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Accounts made before confirmation existed keep signing in; only new registrations wait for the link.
            migrationBuilder.Sql("UPDATE Accounts SET EmailConfirmed = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmTokenExpires",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "ConfirmTokenHash",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "Accounts");
        }
    }
}
