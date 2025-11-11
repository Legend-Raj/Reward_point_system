-- Quick SQL script to add test points to users
-- Run this in SQL Server Management Studio

-- First, check existing users
SELECT TOP 10 
    Id,
    Email_Value,
    EmployeeId_Value,
    TotalPoints,
    LockedPoints,
    IsActive,
    UserType
FROM [Users]
WHERE UserType = 'User' AND IsActive = 1
ORDER BY TotalPoints DESC;

-- Update first 3 users with different points (simpler approach)
WITH RankedUsers AS (
    SELECT 
        Id,
        ROW_NUMBER() OVER (ORDER BY CreatedAt ASC) AS RowNum
    FROM [Users]
    WHERE UserType = 'User' AND IsActive = 1
)
UPDATE u
SET 
    u.TotalPoints = CASE 
        WHEN r.RowNum = 1 THEN 5000
        WHEN r.RowNum = 2 THEN 3000
        WHEN r.RowNum = 3 THEN 2000
        ELSE u.TotalPoints
    END,
    u.UpdatedAt = GETUTCDATE()
FROM [Users] u
INNER JOIN RankedUsers r ON u.Id = r.Id
WHERE r.RowNum <= 3;

-- Verify the update
SELECT TOP 3
    Id,
    Email_Value,
    EmployeeId_Value,
    TotalPoints,
    LockedPoints,
    IsActive
FROM [Users]
WHERE UserType = 'User' AND IsActive = 1
ORDER BY TotalPoints DESC;


