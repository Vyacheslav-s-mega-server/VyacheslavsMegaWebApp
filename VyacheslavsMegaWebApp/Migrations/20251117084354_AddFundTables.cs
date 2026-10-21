using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VyacheslavsMegaWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddFundTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    SnapshotDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundSnapshots", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FundTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Note = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundTransactions", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS InsertFundSnapshot;");

            migrationBuilder.Sql(@"
                CREATE PROCEDURE InsertFundSnapshot()
                BEGIN
                    INSERT INTO FundSnapshots (SnapshotDate, Balance)
                    SELECT
                        DATE_FORMAT(CURRENT_DATE, '%Y-%m-01'),
                        SUM(amount)
                    FROM FundTransactions;
                END;");

            migrationBuilder.Sql(@"
                CREATE EVENT IF NOT EXISTS CreateMonthlySnapshot
                ON SCHEDULE EVERY 1 MONTH
                STARTS CONCAT(DATE_FORMAT(CURRENT_DATE ,'%Y-%m-01'), ' 00:00:00')
                DO
                    CALL InsertFundSnapshot();");

            migrationBuilder.Sql("SET GLOBAL event_scheduler = ON;");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d4bc75a9-ca48-45aa-a3f2-2ea5886ce303",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "e364ff1f-83b7-45ab-9248-40d075d1d24b", "AQAAAAIAAYagAAAAEAo5GMv5/DM7nvmOCNQcWu47nNlxyLYlFaxBAFQfHRzF7KU1oxSeAAEkJT+uLerEjA==", "ba54d010-9bb1-47d7-8754-64539d01bdb1" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundSnapshots");

            migrationBuilder.DropTable(
                name: "FundTransactions");

            migrationBuilder.Sql(@"
                DROP EVENT IF EXISTS CreateMonthlySnapshot;
                DROP PROCEDURE IF EXISTS InsertFundSnapshot;
            ");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d4bc75a9-ca48-45aa-a3f2-2ea5886ce303",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "932c242a-61fa-4fa6-8cf6-d8474089f3ec", "AQAAAAIAAYagAAAAEDUHF5y++2bQziyGnqwzKXBVe68/jRqx28a+N5fTGaJ+8NP/XqyIxzQUe/TXb6oFBw==", "001a8bd8-f8c4-409b-8e08-92f4dacdc426" });
        }
    }
}
