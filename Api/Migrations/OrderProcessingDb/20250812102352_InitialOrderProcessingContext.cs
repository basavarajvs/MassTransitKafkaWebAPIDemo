using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.OrderProcessingDb
{
    /// <inheritdoc />
    public partial class InitialOrderProcessingContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderProcessingSagaStates",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrentState = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OriginalMessage = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalMessageJson = table.Column<string>(type: "TEXT", nullable: true),
                    OrderCreateRetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderCreatedApiCalled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrderCreateResponse = table.Column<string>(type: "TEXT", nullable: true),
                    OrderProcessRetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderProcessedApiCalled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrderProcessResponse = table.Column<string>(type: "TEXT", nullable: true),
                    OrderShipRetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderShippedApiCalled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrderShipResponse = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderProcessingSagaStates", x => x.CorrelationId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderProcessingSagaStates");
        }
    }
}
