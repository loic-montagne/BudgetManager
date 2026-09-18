using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetCategoryOrderInBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetCategoryAssociation",
                columns: table => new
                {
                    BudgetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetCategoryAssociation", x => new { x.BudgetId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_BudgetCategoryAssociation_BudgetCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "BudgetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetCategoryAssociation_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetCategoryAssociation_CategoryId",
                table: "BudgetCategoryAssociation",
                column: "CategoryId");

            migrationBuilder.Sql(@"
INSERT INTO BudgetCategoryAssociation
(
    BudgetId,
    CategoryId,
    [Order]
)
SELECT
    BudgetsId,
    CategoriesId,
    ROW_NUMBER() OVER (
        PARTITION BY BudgetsId
        ORDER BY CategoriesId
    ) - 1
FROM BudgetBudgetCategory;");

            migrationBuilder.DropTable(
                name: "BudgetBudgetCategory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetBudgetCategory",
                columns: table => new
                {
                    BudgetsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetBudgetCategory", x => new { x.BudgetsId, x.CategoriesId });
                    table.ForeignKey(
                        name: "FK_BudgetBudgetCategory_BudgetCategories_CategoriesId",
                        column: x => x.CategoriesId,
                        principalTable: "BudgetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetBudgetCategory_Budgets_BudgetsId",
                        column: x => x.BudgetsId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetBudgetCategory_CategoriesId",
                table: "BudgetBudgetCategory",
                column: "CategoriesId");

            migrationBuilder.Sql(@"
INSERT INTO BudgetBudgetCategory
(
    BudgetsId,
    CategoriesId
)
SELECT
    BudgetId,
    CategoryId
FROM BudgetCategoryAssociation;");

            migrationBuilder.DropTable(
                name: "BudgetCategoryAssociation");
        }
    }
}
