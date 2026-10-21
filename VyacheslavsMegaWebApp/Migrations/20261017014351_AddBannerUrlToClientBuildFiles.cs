using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VyacheslavsMegaWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerUrlToClientBuildFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "ClientBuildFiles",
                type: "longtext",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "ClientBuildFiles");

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
        }
    }
}
