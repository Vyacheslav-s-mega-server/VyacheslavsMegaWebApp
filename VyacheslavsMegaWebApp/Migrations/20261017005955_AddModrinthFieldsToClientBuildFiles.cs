using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VyacheslavsMegaWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddModrinthFieldsToClientBuildFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "SessionKeyHash",
                table: "LauncherSessions",
                type: "varbinary(3072)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "longblob",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IconUrl",
                table: "ClientBuildFiles",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModrinthSlug",
                table: "ClientBuildFiles",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "ClientBuildFiles",
                type: "longtext",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "03d4ba1e-afef-4e77-849d-295bd86148db",
                column: "ConcurrencyStamp",
                value: "9c86397c-1bae-44c0-b1d2-874c7ad39c7d");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "db158bac-48d4-47c3-a6b0-c6d8b5c9ee55",
                column: "ConcurrencyStamp",
                value: "c9588b0c-4dc7-4b69-8f61-9c5f55491856");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d4bc75a9-ca48-45aa-a3f2-2ea5886ce303",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "9447ef93-12be-4ed8-89d5-6f2494e37768", "AQAAAAIAAYagAAAAEEal3jbCeWNXppfboyF0FdCzKMVKKH3V9uKDX3H7xDx4EQyDOoDypNsxwAtc2csELg==", "eac37163-0244-4819-a210-58f0ac900c1a" });

            migrationBuilder.CreateIndex(
                name: "IX_LauncherSessions_SessionKeyHash",
                table: "LauncherSessions",
                column: "SessionKeyHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LauncherSessions_SessionKeyHash",
                table: "LauncherSessions");

            migrationBuilder.DropColumn(
                name: "IconUrl",
                table: "ClientBuildFiles");

            migrationBuilder.DropColumn(
                name: "ModrinthSlug",
                table: "ClientBuildFiles");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "ClientBuildFiles");

            migrationBuilder.AlterColumn<byte[]>(
                name: "SessionKeyHash",
                table: "LauncherSessions",
                type: "longblob",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(3072)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "03d4ba1e-afef-4e77-849d-295bd86148db",
                column: "ConcurrencyStamp",
                value: "93be00d7-848a-4096-aa47-a77de16ba3b7");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "db158bac-48d4-47c3-a6b0-c6d8b5c9ee55",
                column: "ConcurrencyStamp",
                value: "1332e53a-1128-4026-a736-02d3a746aea6");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d4bc75a9-ca48-45aa-a3f2-2ea5886ce303",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "820045f5-4da5-49ff-a0b9-be470b99de50", "AQAAAAIAAYagAAAAEOzhpzGzZixJ4x4Tfgo8f99JBG5Y2HoGPe4OrSRYDtUVrDonlHDlYd9pka3FDYbvbg==", "d94d396f-9ccc-491a-ac5f-9e85b17871a1" });
        }
    }
}
