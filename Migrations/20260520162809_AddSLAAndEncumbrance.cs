using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProVMSIT15.Migrations
{
    /// <inheritdoc />
    public partial class AddSLAAndEncumbrance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Vendors",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Vendors",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "PurchaseRequisitions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinanceSubmittedAt",
                table: "PurchaseRequisitions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEncumbered",
                table: "PurchaseRequisitions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "POIssuedAt",
                table: "PurchaseRequisitions",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "FinanceSubmittedAt",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "IsEncumbered",
                table: "PurchaseRequisitions");

            migrationBuilder.DropColumn(
                name: "POIssuedAt",
                table: "PurchaseRequisitions");
        }
    }
}
