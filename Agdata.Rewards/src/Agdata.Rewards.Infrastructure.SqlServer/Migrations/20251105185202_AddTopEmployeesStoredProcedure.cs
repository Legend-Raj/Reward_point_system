using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agdata.Rewards.Infrastructure.SqlServer.Migrations
{
    public partial class AddTopEmployeesStoredProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE PROCEDURE [dbo].[sp_GetTop3EmployeesWithHighestRewards]
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    SELECT TOP 3
                        u.[Id],
                        u.[Email_Value],
                        u.[EmployeeId_Value],
                        u.[Name_FirstName],
                        u.[Name_MiddleName],
                        u.[Name_LastName],
                        u.[TotalPoints],
                        u.[LockedPoints],
                        u.[IsActive],
                        u.[CreatedAt],
                        u.[UpdatedAt],
                        u.[RowVersion],
                        u.[UserType]
                    FROM [Users] u
                    WHERE u.[UserType] = 'User'
                        AND u.[IsActive] = 1
                    ORDER BY u.[TotalPoints] DESC;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_GetTop3EmployeesWithHighestRewards];");
        }
    }
}
