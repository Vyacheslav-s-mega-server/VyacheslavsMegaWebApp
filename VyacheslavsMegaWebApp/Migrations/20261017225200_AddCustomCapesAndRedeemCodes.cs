using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VyacheslavsMegaWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomCapesAndRedeemCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomCapes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Slug = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    TextureHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    IsPublic = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomCapes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CapeRedeemCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CapeId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    IsUsed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    RedeemedByUserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    RedeemedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapeRedeemCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapeRedeemCodes_CustomCapes_CapeId",
                        column: x => x.CapeId,
                        principalTable: "CustomCapes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CapeRedeemCodes_AspNetUsers_RedeemedByUserId",
                        column: x => x.RedeemedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserCapeOwnerships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    CapeId = table.Column<int>(type: "int", nullable: false),
                    AcquiredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AcquiredVia = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCapeOwnerships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCapeOwnerships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCapeOwnerships_CustomCapes_CapeId",
                        column: x => x.CapeId,
                        principalTable: "CustomCapes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomCapes_Slug",
                table: "CustomCapes",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CapeRedeemCodes_CapeId",
                table: "CapeRedeemCodes",
                column: "CapeId");

            migrationBuilder.CreateIndex(
                name: "IX_CapeRedeemCodes_Code",
                table: "CapeRedeemCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CapeRedeemCodes_RedeemedByUserId",
                table: "CapeRedeemCodes",
                column: "RedeemedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCapeOwnerships_CapeId",
                table: "UserCapeOwnerships",
                column: "CapeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCapeOwnerships_UserId_CapeId",
                table: "UserCapeOwnerships",
                columns: new[] { "UserId", "CapeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserCapeOwnerships");
            migrationBuilder.DropTable(name: "CapeRedeemCodes");
            migrationBuilder.DropTable(name: "CustomCapes");
        }
    }
}
