DECLARE @UserEmail nvarchar(256) = 'wcambronero@itqscr.com';
DECLARE @Today date = CAST(GETDATE() AS date);

;WITH Management AS
(
    SELECT
        COUNT(1) AS ManagementAlerts,
        SUM(CASE 
                WHEN Active = 1 
                 AND AssignedEmail = @UserEmail 
                THEN 1 ELSE 0 
            END) AS AssignedToMe,
        SUM(CASE 
                WHEN Active = 1 
                 AND (AssignedEmail IS NULL OR LTRIM(RTRIM(AssignedEmail)) = '') 
                THEN 1 ELSE 0 
            END) AS Unassigned,
        SUM(CASE 
                WHEN Active = 0 
                 AND CAST(ISNULL(UpdatedAt, ResolveTime) AS date) = @Today
                THEN 1 ELSE 0 
            END) AS ResolvedToday
    FROM dbo.AlertsManagement
    WHERE Active = 1
       OR (
            Active = 0 
            AND CAST(ISNULL(UpdatedAt, ResolveTime) AS date) = @Today
          )
),
Backup AS
(
    SELECT
        COUNT(1) AS BackupAlerts
    FROM dbo.AlertsBackup
    WHERE Active = 1
)
SELECT
    ISNULL(m.ManagementAlerts, 0) + ISNULL(b.BackupAlerts, 0) AS TotalAlerts,
    ISNULL(b.BackupAlerts, 0) AS BackupAlerts,
    ISNULL(m.ManagementAlerts, 0) AS ManagementAlerts,
    ISNULL(m.AssignedToMe, 0) AS AssignedToMe,
    ISNULL(m.Unassigned, 0) AS Unassigned,
    ISNULL(m.ResolvedToday, 0) AS ResolvedToday
FROM Management m
CROSS JOIN Backup b;
