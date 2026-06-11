using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_mvc.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyUserAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionExpiresAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SessionTokenHash",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SessionExpiresAtUtc",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionTokenHash",
                table: "Users",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
