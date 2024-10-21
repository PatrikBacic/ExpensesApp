using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ExpensesApp.Data.Migrations
{
    public partial class CreateCategoryAndTransaction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
            name: "Amount",
            table: "Transactions",
            type: "decimal(18,2)",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
            name: "Amount",
            table: "Transactions",
            type: "int",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)");
        }
    }
}
