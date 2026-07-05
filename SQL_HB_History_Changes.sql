CREATE TABLE dbo.ReporteBeneficioHibridoHistory
(
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    SnapshotRunId UNIQUEIDENTIFIER NOT NULL,
    ResourceKey NVARCHAR(500) NOT NULL,

    Customer NVARCHAR(300) NULL,
    TenantId NVARCHAR(100) NULL,
    SubscriptionId NVARCHAR(100) NULL,
    Subscription NVARCHAR(300) NULL,
    ResourceGroup NVARCHAR(300) NULL,
    ResourceName NVARCHAR(300) NULL,

    HasWindowsAHUB BIT NOT NULL DEFAULT 0,
    HasSqlAHUB BIT NOT NULL DEFAULT 0,
    HasTagHB BIT NOT NULL DEFAULT 0,
    HasTagHBSQL BIT NOT NULL DEFAULT 0,

    TagHB NVARCHAR(300) NULL,
    TagHBSQL NVARCHAR(300) NULL,

    FirstSeenDate DATETIME2 NULL,
    LastSeenDate DATETIME2 NULL,
    SnapshotDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_RBHHistory_ResourceKey
ON dbo.ReporteBeneficioHibridoHistory(ResourceKey, SnapshotDate DESC);
GO

CREATE INDEX IX_RBHHistory_SnapshotRunId
ON dbo.ReporteBeneficioHibridoHistory(SnapshotRunId);
GO


CREATE TABLE dbo.ReporteBeneficioHibridoChanges
(
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    SnapshotRunId UNIQUEIDENTIFIER NOT NULL,
    ResourceKey NVARCHAR(500) NOT NULL,

    Customer NVARCHAR(300) NULL,
    TenantId NVARCHAR(100) NULL,
    SubscriptionId NVARCHAR(100) NULL,
    Subscription NVARCHAR(300) NULL,
    ResourceGroup NVARCHAR(300) NULL,
    ResourceName NVARCHAR(300) NULL,

    ChangeType NVARCHAR(100) NOT NULL,
    OldValue NVARCHAR(300) NULL,
    NewValue NVARCHAR(300) NULL,

    ChangeDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_RBHChanges_ResourceKey
ON dbo.ReporteBeneficioHibridoChanges(ResourceKey, ChangeDate DESC);
GO

CREATE INDEX IX_RBHChanges_ChangeDate
ON dbo.ReporteBeneficioHibridoChanges(ChangeDate DESC);
GO