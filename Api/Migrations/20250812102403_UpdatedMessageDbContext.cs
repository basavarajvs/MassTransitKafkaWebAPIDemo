using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedMessageDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SagaStates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SagaStates",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CurrentState = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OrderCreateResponse = table.Column<string>(type: "TEXT", nullable: true),
                    OrderCreateRetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderCreatedApiCalled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrderProcessResponse = table.Column<string>(type: "TEXT", nullable: true),
                    OrderProcessRetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderProcessedApiCalled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrderShipResponse = table.Column<string>(type: "TEXT", nullable: true),
                    OrderShipRetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderShippedApiCalled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OriginalMessage = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalMessageJson = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagaStates", x => x.CorrelationId);
                });
        }
    }
}
