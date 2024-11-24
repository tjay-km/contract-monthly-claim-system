using Microsoft.EntityFrameworkCore.Migrations;

namespace CMCS.Data.Migrations
{
    public partial class AddMoreDepartmentsAndModules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new departments
            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Name", "Code", "IsActive" },
                values: new object[,]
                {
                    { 2, "Information Technology", "IT", true },
                    { 3, "Mathematics", "MATH", true },
                    { 4, "Physics", "PHYS", true },
                    { 5, "Chemistry", "CHEM", true },
                    { 6, "Biology", "BIO", true },
                    { 7, "Engineering", "ENG", true },
                    { 8, "Business Management", "BUS", true },
                    { 9, "Economics", "ECON", true },
                    { 10, "Psychology", "PSYC", true }
                });

            // Add new modules
            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "Code", "Name", "DepartmentId", "IsActive" },
                values: new object[,]
                {
                    // IT Modules
                    { 101, "IT101", "Introduction to Programming", 2, true },
                    { 102, "IT201", "Database Management", 2, true },
                    { 103, "IT301", "Web Development", 2, true },
                    { 104, "IT401", "Network Security", 2, true },

                    // Mathematics Modules
                    { 201, "MTH101", "Calculus I", 3, true },
                    { 202, "MTH201", "Linear Algebra", 3, true },
                    { 203, "MTH301", "Abstract Algebra", 3, true },
                    { 204, "MTH401", "Real Analysis", 3, true },

                    // Physics Modules
                    { 301, "PHY101", "Mechanics", 4, true },
                    { 302, "PHY201", "Electromagnetism", 4, true },
                    { 303, "PHY301", "Quantum Physics", 4, true },
                    { 304, "PHY401", "Thermodynamics", 4, true },

                    // Chemistry Modules
                    { 401, "CHM101", "General Chemistry", 5, true },
                    { 402, "CHM201", "Organic Chemistry", 5, true },
                    { 403, "CHM301", "Physical Chemistry", 5, true },
                    { 404, "CHM401", "Analytical Chemistry", 5, true },

                    // Biology Modules
                    { 501, "BIO101", "Cell Biology", 6, true },
                    { 502, "BIO201", "Genetics", 6, true },
                    { 503, "BIO301", "Ecology", 6, true },
                    { 504, "BIO401", "Molecular Biology", 6, true },

                    // Engineering Modules
                    { 601, "ENG101", "Engineering Mathematics", 7, true },
                    { 602, "ENG201", "Mechanics of Materials", 7, true },
                    { 603, "ENG301", "Control Systems", 7, true },
                    { 604, "ENG401", "Engineering Design", 7, true },

                    // Business Management Modules
                    { 701, "BUS101", "Introduction to Business", 8, true },
                    { 702, "BUS201", "Marketing Management", 8, true },
                    { 703, "BUS301", "Operations Management", 8, true },
                    { 704, "BUS401", "Strategic Management", 8, true },

                    // Economics Modules
                    { 801, "ECN101", "Microeconomics", 9, true },
                    { 802, "ECN201", "Macroeconomics", 9, true },
                    { 803, "ECN301", "International Economics", 9, true },
                    { 804, "ECN401", "Development Economics", 9, true },

                    // Psychology Modules
                    { 901, "PSY101", "Introduction to Psychology", 10, true },
                    { 902, "PSY201", "Developmental Psychology", 10, true },
                    { 903, "PSY301", "Cognitive Psychology", 10, true },
                    { 904, "PSY401", "Clinical Psychology", 10, true }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the added modules
            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValues: new object[] { 101, 102, 103, 104, 201, 202, 203, 204, 301, 302, 303, 304, 401, 402, 403, 404, 501, 502, 503, 504, 601, 602, 603, 604, 701, 702, 703, 704, 801, 802, 803, 804, 901, 902, 903, 904 });

            // Remove the added departments
            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValues: new object[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        }
    }
}
