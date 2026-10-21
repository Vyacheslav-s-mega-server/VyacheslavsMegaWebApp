using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VyacheslavsMegaWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddMicrosoftLicenseLinkToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasMinecraftLicense",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MicrosoftGamertag",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MicrosoftLicenseLinkedAt",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MicrosoftUuid",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnedMojangCapesJson",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "03d4ba1e-afef-4e77-849d-295bd86148db",
                column: "ConcurrencyStamp",
                value: "1ca40dc8-422c-428f-b186-336c8bba3e24");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "db158bac-48d4-47c3-a6b0-c6d8b5c9ee55",
                column: "ConcurrencyStamp",
                value: "e4630e31-70bf-445c-a63a-676107d81fa2");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d4bc75a9-ca48-45aa-a3f2-2ea5886ce303",
                columns: new[] { "ConcurrencyStamp", "HasMinecraftLicense", "MicrosoftGamertag", "MicrosoftLicenseLinkedAt", "MicrosoftUuid", "OwnedMojangCapesJson", "PasswordHash", "SecurityStamp" },
                values: new object[] { "541233d2-f731-4834-a48a-18558f4f30dd", false, null, null, null, null, "AQAAAAIAAYagAAAAEPxx6suPp5puiqTy6xQjnKhzHZ0/a190bopLFZGWihTjJYzoY1baa2cliJRgZCdaEA==", "40278ad1-d87d-41c2-9307-3d528c4fd9fa" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasMinecraftLicense",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MicrosoftGamertag",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MicrosoftLicenseLinkedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MicrosoftUuid",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OwnedMojangCapesJson",
                table: "AspNetUsers");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "03d4ba1e-afef-4e77-849d-295bd86148db",
                column: "ConcurrencyStamp",
                value: "79f7f172-0503-48f1-874b-cc89529855e1");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "db158bac-48d4-47c3-a6b0-c6d8b5c9ee55",
                column: "ConcurrencyStamp",
                value: "f8cba131-04ed-49e2-a43a-734a8402b508");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d4bc75a9-ca48-45aa-a3f2-2ea5886ce303",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "c1e68228-516a-472f-b558-61aa8ddc1988", "AQAAAAIAAYagAAAAEEtV3zknA1WxTGxYJBH0ssOE3lm57qe9qiaO22lXYFY7kdZH9XJwCJXBDI6+nW3Law==", "4a95bb12-0d11-45b6-9530-7594816da044" });
        }
    }
}
