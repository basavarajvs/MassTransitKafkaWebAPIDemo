using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCleanMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StepData = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SagaStates",
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
                    table.PrimaryKey("PK_SagaStates", x => x.CorrelationId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "SagaStates");
        }
    }
}
