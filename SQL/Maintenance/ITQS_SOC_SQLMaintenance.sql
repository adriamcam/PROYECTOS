IF OBJECT_ID('dbo.ITQS_SOC_SQLMaintenanceHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ITQS_SOC_SQLMaintenanceHistory
    (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ExecutedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        TableName NVARCHAR(128) NOT NULL,
        ActionName NVARCHAR(128) NOT NULL,
        RetentionDays INT NOT NULL,
        BatchSize INT NOT NULL,
        RowsAffected BIGINT NOT NULL,
        Succeeded BIT NOT NULL,
        Message NVARCHAR(4000) NULL,
        StartedAt DATETIME2 NOT NULL,
        FinishedAt DATETIME2 NOT NULL,
        ExecutedBy NVARCHAR(320) NULL,
        ExecutedByEmail NVARCHAR(320) NULL
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.ITQS_SOC_sp_SQLMaintenance_GetDashboard
    @RetentionDays INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    IF @RetentionDays < 30 SET @RetentionDays = 30;
    DECLARE @Cutoff DATETIME2 = DATEADD(DAY, -@RetentionDays, SYSDATETIME());

    ;WITH TableRows AS
    (
        SELECT TableName='AlertsManagement', TotalRows=COUNT_BIG(1) FROM dbo.AlertsManagement
        UNION ALL SELECT 'AlertasBackup', COUNT_BIG(1) FROM dbo.AlertasBackup
        UNION ALL SELECT 'AzureAlertCloseQueue', COUNT_BIG(1) FROM dbo.AzureAlertCloseQueue
        UNION ALL SELECT 'AlertUpdatesHistory', COUNT_BIG(1) FROM dbo.AlertUpdatesHistory
    ),
    TableSpace AS
    (
        SELECT TableName=t.name, TotalSpaceMb=CAST(SUM(a.total_pages)*8.0/1024 AS DECIMAL(18,2))
        FROM sys.tables t
        JOIN sys.indexes i ON t.object_id=i.object_id
        JOIN sys.partitions p ON i.object_id=p.object_id AND i.index_id=p.index_id
        JOIN sys.allocation_units a ON p.partition_id=a.container_id
        WHERE t.name IN ('AlertsManagement','AlertasBackup','AzureAlertCloseQueue','AlertUpdatesHistory')
        GROUP BY t.name
    ),
    Eligible AS
    (
        SELECT TableName='AlertsManagement', EligibleToDelete=COUNT_BIG(1) FROM dbo.AlertsManagement WHERE ISNULL(Active,0)=0 AND ISNULL(AlertStatus,'')='Closed' AND COALESCE(ResolveTime,UpdatedAt,InsertedAt)<@Cutoff
        UNION ALL SELECT 'AzureAlertCloseQueue', COUNT_BIG(1) FROM dbo.AzureAlertCloseQueue WHERE ISNULL(Status,'') IN ('Processed','NotFound','NoCustomerContext','Conflict','Failed','Skipped') AND COALESCE(ProcessedAt,RequestedAt)<@Cutoff
        UNION ALL SELECT 'AlertasBackup', COUNT_BIG(1) FROM dbo.AlertasBackup WHERE ISNULL(Active,0)=0 AND COALESCE(ResolveTime,UpdatedAt,InsertedAt,AlertTime)<@Cutoff
        UNION ALL SELECT 'AlertUpdatesHistory', CAST(0 AS BIGINT)
    )
    SELECT
        DatabaseName=DB_NAME(),
        TotalSpaceMb=ISNULL((SELECT SUM(TotalSpaceMb) FROM TableSpace),0),
        TotalRows=ISNULL((SELECT SUM(TotalRows) FROM TableRows),0),
        EstimatedRecoverableMb=CAST(ISNULL((SELECT SUM(CASE WHEN r.TotalRows>0 THEN (s.TotalSpaceMb/r.TotalRows)*e.EligibleToDelete ELSE 0 END) FROM Eligible e JOIN TableRows r ON r.TableName=e.TableName JOIN TableSpace s ON s.TableName=e.TableName),0) AS DECIMAL(18,2)),
        HealthStatus=CASE WHEN ISNULL((SELECT SUM(EligibleToDelete) FROM Eligible),0)>100000 THEN 'Warning' ELSE 'Saludable' END,
        LastMaintenanceText=ISNULL((SELECT TOP(1) CONVERT(NVARCHAR(30),ExecutedAt,103)+' '+CONVERT(NVARCHAR(5),ExecutedAt,108) FROM dbo.ITQS_SOC_SQLMaintenanceHistory WHERE Succeeded=1 ORDER BY ExecutedAt DESC),'Nunca');

    SELECT TableName='AlertsManagement',DisplayName='AlertsManagement',Description='Tabla principal de alertas del sistema',TotalRows=COUNT_BIG(1),ActiveRows=SUM(CASE WHEN ISNULL(Active,0)=1 THEN 1 ELSE 0 END),ClosedRows=SUM(CASE WHEN ISNULL(Active,0)=0 AND ISNULL(AlertStatus,'')='Closed' THEN 1 ELSE 0 END),PendingRows=CAST(0 AS BIGINT),ProcessedRows=CAST(0 AS BIGINT),NotFoundRows=CAST(0 AS BIGINT),RetryRows=CAST(0 AS BIGINT),ErrorRows=CAST(0 AS BIGINT),EligibleToDelete=SUM(CASE WHEN ISNULL(Active,0)=0 AND ISNULL(AlertStatus,'')='Closed' AND COALESCE(ResolveTime,UpdatedAt,InsertedAt)<@Cutoff THEN 1 ELSE 0 END),TableSizeMb=ISNULL((SELECT TotalSpaceMb FROM TableSpace WHERE TableName='AlertsManagement'),0),EstimatedRecoverableMb=CAST(0 AS DECIMAL(18,2)),IsProtected=CAST(0 AS bit),LastCleanupText=ISNULL((SELECT TOP(1) CONVERT(NVARCHAR(30),ExecutedAt,103)+' '+CONVERT(NVARCHAR(5),ExecutedAt,108) FROM dbo.ITQS_SOC_SQLMaintenanceHistory WHERE TableName='AlertsManagement' AND Succeeded=1 ORDER BY ExecutedAt DESC),'Nunca'),RetentionRule=CONCAT('Eliminar alertas cerradas con más de ',@RetentionDays,' días.'),RecommendedAction='Conservar alertas activas y cerradas recientes.' FROM dbo.AlertsManagement
    UNION ALL
    SELECT TableName='AzureAlertCloseQueue',DisplayName='AzureAlertCloseQueue',Description='Cola de cierres de alertas para integración con Azure',TotalRows=COUNT_BIG(1),ActiveRows=CAST(0 AS BIGINT),ClosedRows=CAST(0 AS BIGINT),PendingRows=SUM(CASE WHEN ISNULL(Status,'')='Pending' THEN 1 ELSE 0 END),ProcessedRows=SUM(CASE WHEN ISNULL(Status,'')='Processed' THEN 1 ELSE 0 END),NotFoundRows=SUM(CASE WHEN ISNULL(Status,'')='NotFound' THEN 1 ELSE 0 END),RetryRows=SUM(CASE WHEN ISNULL(Status,'')='Retry' THEN 1 ELSE 0 END),ErrorRows=SUM(CASE WHEN ISNULL(Status,'') IN ('Error','Failed','Conflict','NoCustomerContext') THEN 1 ELSE 0 END),EligibleToDelete=SUM(CASE WHEN ISNULL(Status,'') IN ('Processed','NotFound','NoCustomerContext','Conflict','Failed','Skipped') AND COALESCE(ProcessedAt,RequestedAt)<@Cutoff THEN 1 ELSE 0 END),TableSizeMb=ISNULL((SELECT TotalSpaceMb FROM TableSpace WHERE TableName='AzureAlertCloseQueue'),0),EstimatedRecoverableMb=CAST(0 AS DECIMAL(18,2)),IsProtected=CAST(0 AS bit),LastCleanupText=ISNULL((SELECT TOP(1) CONVERT(NVARCHAR(30),ExecutedAt,103)+' '+CONVERT(NVARCHAR(5),ExecutedAt,108) FROM dbo.ITQS_SOC_SQLMaintenanceHistory WHERE TableName='AzureAlertCloseQueue' AND Succeeded=1 ORDER BY ExecutedAt DESC),'Nunca'),RetentionRule=CONCAT('Eliminar estados finales con más de ',@RetentionDays,' días.'),RecommendedAction='No eliminar Pending ni Retry.' FROM dbo.AzureAlertCloseQueue
    UNION ALL
    SELECT TableName='AlertasBackup',DisplayName='AlertasBackup',Description='Respaldo histórico de alertas',TotalRows=COUNT_BIG(1),ActiveRows=SUM(CASE WHEN ISNULL(Active,0)=1 THEN 1 ELSE 0 END),ClosedRows=SUM(CASE WHEN ISNULL(Active,0)=0 THEN 1 ELSE 0 END),PendingRows=CAST(0 AS BIGINT),ProcessedRows=CAST(0 AS BIGINT),NotFoundRows=CAST(0 AS BIGINT),RetryRows=CAST(0 AS BIGINT),ErrorRows=CAST(0 AS BIGINT),EligibleToDelete=SUM(CASE WHEN ISNULL(Active,0)=0 AND COALESCE(ResolveTime,UpdatedAt,InsertedAt,AlertTime)<@Cutoff THEN 1 ELSE 0 END),TableSizeMb=ISNULL((SELECT TotalSpaceMb FROM TableSpace WHERE TableName='AlertasBackup'),0),EstimatedRecoverableMb=CAST(0 AS DECIMAL(18,2)),IsProtected=CAST(0 AS bit),LastCleanupText=ISNULL((SELECT TOP(1) CONVERT(NVARCHAR(30),ExecutedAt,103)+' '+CONVERT(NVARCHAR(5),ExecutedAt,108) FROM dbo.ITQS_SOC_SQLMaintenanceHistory WHERE TableName='AlertasBackup' AND Succeeded=1 ORDER BY ExecutedAt DESC),'Nunca'),RetentionRule=CONCAT('Eliminar alertas Backup inactivas con más de ',@RetentionDays,' días.'),RecommendedAction='Conservar alertas Backup activas y recientes.' FROM dbo.AlertasBackup
    UNION ALL
    SELECT TableName='AlertUpdatesHistory',DisplayName='AlertUpdatesHistory',Description='Historial de actualizaciones de alertas',TotalRows=COUNT_BIG(1),ActiveRows=CAST(0 AS BIGINT),ClosedRows=CAST(0 AS BIGINT),PendingRows=CAST(0 AS BIGINT),ProcessedRows=CAST(0 AS BIGINT),NotFoundRows=CAST(0 AS BIGINT),RetryRows=CAST(0 AS BIGINT),ErrorRows=CAST(0 AS BIGINT),EligibleToDelete=CAST(0 AS BIGINT),TableSizeMb=ISNULL((SELECT TotalSpaceMb FROM TableSpace WHERE TableName='AlertUpdatesHistory'),0),EstimatedRecoverableMb=CAST(0 AS DECIMAL(18,2)),IsProtected=CAST(1 AS bit),LastCleanupText='N/A',RetentionRule='Historial protegido.',RecommendedAction='Este historial no se elimina desde este módulo.' FROM dbo.AlertUpdatesHistory;

    SELECT TOP(10) Id,ExecutedAt,TableName,ActionName,RowsAffected,ExecutedBy=ISNULL(ExecutedBy,''),ExecutedByEmail=ISNULL(ExecutedByEmail,''),Succeeded,Message=ISNULL(Message,'') FROM dbo.ITQS_SOC_SQLMaintenanceHistory ORDER BY ExecutedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ITQS_SOC_sp_SQLMaintenance_CleanupAlertsManagement @RetentionDays INT=30,@BatchSize INT=5000,@UserEmail NVARCHAR(320)=NULL,@UserName NVARCHAR(320)=NULL AS
BEGIN
SET NOCOUNT ON; IF @RetentionDays<30 SET @RetentionDays=30; IF @BatchSize IS NULL OR @BatchSize<=0 SET @BatchSize=5000; IF @BatchSize>20000 SET @BatchSize=20000;
DECLARE @StartedAt DATETIME2=SYSDATETIME(),@Rows BIGINT=0,@Deleted INT=1,@Cutoff DATETIME2=DATEADD(DAY,-@RetentionDays,SYSDATETIME());
WHILE @Deleted>0 BEGIN DELETE TOP(@BatchSize) FROM dbo.AlertsManagement WHERE ISNULL(Active,0)=0 AND ISNULL(AlertStatus,'')='Closed' AND COALESCE(ResolveTime,UpdatedAt,InsertedAt)<@Cutoff; SET @Deleted=@@ROWCOUNT; SET @Rows+=@Deleted; IF @Deleted>0 WAITFOR DELAY '00:00:01'; END;
UPDATE STATISTICS dbo.AlertsManagement;
INSERT INTO dbo.ITQS_SOC_SQLMaintenanceHistory(TableName,ActionName,RetentionDays,BatchSize,RowsAffected,Succeeded,Message,StartedAt,FinishedAt,ExecutedBy,ExecutedByEmail) VALUES('AlertsManagement','CleanupClosedOlderThanRetention',@RetentionDays,@BatchSize,@Rows,1,CONCAT('Limpieza completada. Registros eliminados: ',@Rows),@StartedAt,SYSDATETIME(),@UserName,@UserEmail);
SELECT TableName='AlertsManagement',ActionName='CleanupClosedOlderThanRetention',RetentionDays=@RetentionDays,BatchSize=@BatchSize,RowsAffected=@Rows,Succeeded=CAST(1 AS bit),Message=CONCAT('Limpieza completada. Registros eliminados: ',@Rows),StartedAt=@StartedAt,FinishedAt=SYSDATETIME(),ExecutedBy=ISNULL(@UserName,''),ExecutedByEmail=ISNULL(@UserEmail,'');
END;
GO

CREATE OR ALTER PROCEDURE dbo.ITQS_SOC_sp_SQLMaintenance_CleanupAzureAlertCloseQueue @RetentionDays INT=30,@BatchSize INT=5000,@UserEmail NVARCHAR(320)=NULL,@UserName NVARCHAR(320)=NULL AS
BEGIN
SET NOCOUNT ON; IF @RetentionDays<30 SET @RetentionDays=30; IF @BatchSize IS NULL OR @BatchSize<=0 SET @BatchSize=5000; IF @BatchSize>20000 SET @BatchSize=20000;
DECLARE @StartedAt DATETIME2=SYSDATETIME(),@Rows BIGINT=0,@Deleted INT=1,@Cutoff DATETIME2=DATEADD(DAY,-@RetentionDays,SYSDATETIME());
WHILE @Deleted>0 BEGIN DELETE TOP(@BatchSize) FROM dbo.AzureAlertCloseQueue WHERE ISNULL(Status,'') IN ('Processed','NotFound','NoCustomerContext','Conflict','Failed','Skipped') AND COALESCE(ProcessedAt,RequestedAt)<@Cutoff; SET @Deleted=@@ROWCOUNT; SET @Rows+=@Deleted; IF @Deleted>0 WAITFOR DELAY '00:00:01'; END;
UPDATE STATISTICS dbo.AzureAlertCloseQueue;
INSERT INTO dbo.ITQS_SOC_SQLMaintenanceHistory(TableName,ActionName,RetentionDays,BatchSize,RowsAffected,Succeeded,Message,StartedAt,FinishedAt,ExecutedBy,ExecutedByEmail) VALUES('AzureAlertCloseQueue','CleanupQueueOlderThanRetention',@RetentionDays,@BatchSize,@Rows,1,CONCAT('Limpieza completada. Registros eliminados: ',@Rows),@StartedAt,SYSDATETIME(),@UserName,@UserEmail);
SELECT TableName='AzureAlertCloseQueue',ActionName='CleanupQueueOlderThanRetention',RetentionDays=@RetentionDays,BatchSize=@BatchSize,RowsAffected=@Rows,Succeeded=CAST(1 AS bit),Message=CONCAT('Limpieza completada. Registros eliminados: ',@Rows),StartedAt=@StartedAt,FinishedAt=SYSDATETIME(),ExecutedBy=ISNULL(@UserName,''),ExecutedByEmail=ISNULL(@UserEmail,'');
END;
GO

CREATE OR ALTER PROCEDURE dbo.ITQS_SOC_sp_SQLMaintenance_CleanupAlertasBackup @RetentionDays INT=30,@BatchSize INT=5000,@UserEmail NVARCHAR(320)=NULL,@UserName NVARCHAR(320)=NULL AS
BEGIN
SET NOCOUNT ON; IF @RetentionDays<30 SET @RetentionDays=30; IF @BatchSize IS NULL OR @BatchSize<=0 SET @BatchSize=5000; IF @BatchSize>20000 SET @BatchSize=20000;
DECLARE @StartedAt DATETIME2=SYSDATETIME(),@Rows BIGINT=0,@Deleted INT=1,@Cutoff DATETIME2=DATEADD(DAY,-@RetentionDays,SYSDATETIME());
WHILE @Deleted>0 BEGIN DELETE TOP(@BatchSize) FROM dbo.AlertasBackup WHERE ISNULL(Active,0)=0 AND COALESCE(ResolveTime,UpdatedAt,InsertedAt,AlertTime)<@Cutoff; SET @Deleted=@@ROWCOUNT; SET @Rows+=@Deleted; IF @Deleted>0 WAITFOR DELAY '00:00:01'; END;
UPDATE STATISTICS dbo.AlertasBackup;
INSERT INTO dbo.ITQS_SOC_SQLMaintenanceHistory(TableName,ActionName,RetentionDays,BatchSize,RowsAffected,Succeeded,Message,StartedAt,FinishedAt,ExecutedBy,ExecutedByEmail) VALUES('AlertasBackup','CleanupInactiveOlderThanRetention',@RetentionDays,@BatchSize,@Rows,1,CONCAT('Limpieza completada. Registros eliminados: ',@Rows),@StartedAt,SYSDATETIME(),@UserName,@UserEmail);
SELECT TableName='AlertasBackup',ActionName='CleanupInactiveOlderThanRetention',RetentionDays=@RetentionDays,BatchSize=@BatchSize,RowsAffected=@Rows,Succeeded=CAST(1 AS bit),Message=CONCAT('Limpieza completada. Registros eliminados: ',@Rows),StartedAt=@StartedAt,FinishedAt=SYSDATETIME(),ExecutedBy=ISNULL(@UserName,''),ExecutedByEmail=ISNULL(@UserEmail,'');
END;
GO
