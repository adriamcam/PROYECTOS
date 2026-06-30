/*
    ITQS Support Operations Center
    Performance Indexes for AssignedAlertsDashboardRepository

    Ejecutar en la base REPORTES.
    Estos índices están diseñados para reducir lecturas en Dashboard, Asignadas a mí,
    cierre de alertas y búsquedas de último historial.
*/

/* 1. Historial: acelera LatestHistory y detalle de alertas */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AlertUpdatesHistory_KPI_Alert_UpdatedAt'
      AND object_id = OBJECT_ID('dbo.AlertUpdatesHistory')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AlertUpdatesHistory_KPI_Alert_UpdatedAt
    ON dbo.AlertUpdatesHistory (KPIType, AlertId, UpdatedAt DESC)
    INCLUDE (Status, Comment, UpdatedBy, UserEmail, Res_norm, Alert_norm);
END;
GO

/* 2. AlertsManagement: alertas sin asignar para Dashboard */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AlertsManagement_Unassigned_Dashboard'
      AND object_id = OBJECT_ID('dbo.AlertsManagement')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AlertsManagement_Unassigned_Dashboard
    ON dbo.AlertsManagement
    (
        Active,
        AssignedEmail,
        SubscriptionName,
        AlertName,
        Severity,
        TargetResourceName
    )
    INCLUDE
    (
        Id,
        UpdatedAt,
        InsertedAt,
        AlertStatus,
        AlertId,
        TenantId,
        SubscriptionId,
        AssignedTo,
        ResourceGroup
    );
END;
GO

/* 3. AlertsManagement: alertas asignadas a un ingeniero */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AlertsManagement_Assigned_User'
      AND object_id = OBJECT_ID('dbo.AlertsManagement')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AlertsManagement_Assigned_User
    ON dbo.AlertsManagement
    (
        Active,
        AssignedEmail,
        SubscriptionName,
        AlertName,
        Severity,
        TargetResourceName
    )
    INCLUDE
    (
        Id,
        UpdatedAt,
        InsertedAt,
        AlertStatus,
        AssignedTo,
        AlertState,
        AlertTime,
        ResourceGroup,
        Details,
        ResolutionNotes,
        TenantId,
        SubscriptionId,
        AlertId
    );
END;
GO

/* 4. AlertasBackup: Dashboard sin asignar */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AlertasBackup_Unassigned_Dashboard'
      AND object_id = OBJECT_ID('dbo.AlertasBackup')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AlertasBackup_Unassigned_Dashboard
    ON dbo.AlertasBackup
    (
        Active,
        AssignedEmail,
        SubscriptionName,
        AlertRule,
        Severity
    )
    INCLUDE
    (
        Id,
        ResourceName,
        VMName,
        ProtectedItem,
        UpdatedAt,
        InsertedAt,
        AlertTime,
        AssignedTo,
        TenantId,
        SubscriptionId,
        ResourceGroup,
        ErrorDetail,
        ResolutionNotes,
        AzureAlertId
    );
END;
GO

/* 5. AlertasBackup: alertas asignadas a un ingeniero */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AlertasBackup_Assigned_User'
      AND object_id = OBJECT_ID('dbo.AlertasBackup')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AlertasBackup_Assigned_User
    ON dbo.AlertasBackup
    (
        Active,
        AssignedEmail,
        SubscriptionName,
        AlertRule,
        Severity
    )
    INCLUDE
    (
        Id,
        ResourceName,
        VMName,
        ProtectedItem,
        UpdatedAt,
        InsertedAt,
        AlertTime,
        AssignedTo,
        TenantId,
        SubscriptionId,
        ResourceGroup,
        ErrorDetail,
        ResolutionNotes,
        AzureAlertId
    );
END;
GO

/* 6. AzureAlertCloseQueue: evita duplicados y acelera NOT EXISTS por AzureAlertId */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AzureAlertCloseQueue_AlertId_Status_RequestedAt'
      AND object_id = OBJECT_ID('dbo.AzureAlertCloseQueue')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AzureAlertCloseQueue_AlertId_Status_RequestedAt
    ON dbo.AzureAlertCloseQueue (AzureAlertId, Status, RequestedAt)
    INCLUDE (RequestId, TenantId, SubscriptionId, UserEmail, SourceTable, AlertRecordId);
END;
GO

/* 7. Mantenimiento pendiente */
ALTER INDEX ALL ON dbo.AlertasBackup REBUILD;
UPDATE STATISTICS dbo.AlertasBackup;
UPDATE STATISTICS dbo.AlertsManagement;
UPDATE STATISTICS dbo.AlertUpdatesHistory;
UPDATE STATISTICS dbo.AzureAlertCloseQueue;
GO
