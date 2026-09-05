SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @MigrationUserId UNIQUEIDENTIFIER = 'A4C019D4-9EEE-4EC5-9D9A-992196906651';

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @MigrationUserId)
BEGIN
    INSERT INTO dbo.Users (Id, Username, Email, PasswordHash, Role, CreatedAt)
    VALUES (
        @MigrationUserId,
        'system_migration',
        'system-migration@local.invalid',
        'MIGRATED_RECORDS_CANNOT_AUTHENTICATE',
        'Admin',
        SYSUTCDATETIME()
    );
END;

IF COL_LENGTH(N'dbo.Purchases', N'CreatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Purchases ADD CreatedByUserId UNIQUEIDENTIFIER NULL;
    UPDATE dbo.Purchases SET CreatedByUserId = @MigrationUserId;
    ALTER TABLE dbo.Purchases ALTER COLUMN CreatedByUserId UNIQUEIDENTIFIER NOT NULL;
    ALTER TABLE dbo.Purchases ADD CONSTRAINT FK_Purchases_Users
        FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id);
    CREATE INDEX IX_Purchases_CreatedByUserId ON dbo.Purchases(CreatedByUserId);
END;

IF COL_LENGTH(N'dbo.Sales', N'ProductId') IS NOT NULL
BEGIN
    CREATE TABLE dbo.Sales_New (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Date DATETIME2 NOT NULL,
        CreatedByUserId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        CONSTRAINT FK_Sales_New_Users FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id)
    );

    CREATE TABLE dbo.SaleLines (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        SaleId UNIQUEIDENTIFIER NOT NULL,
        ProductId UNIQUEIDENTIFIER NOT NULL,
        ProductName NVARCHAR(200) NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_SaleLines_Sales FOREIGN KEY(SaleId) REFERENCES dbo.Sales_New(Id) ON DELETE CASCADE,
        CONSTRAINT FK_SaleLines_Products FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id)
    );

    INSERT INTO dbo.Sales_New (Id, Date, CreatedByUserId, CreatedAt)
    SELECT Id, Date, @MigrationUserId, CreatedAt FROM dbo.Sales;

    INSERT INTO dbo.SaleLines (Id, SaleId, ProductId, ProductName, Quantity, UnitPrice)
    SELECT NEWID(), s.Id, s.ProductId, p.Name, s.Quantity, s.UnitPrice
    FROM dbo.Sales s
    INNER JOIN dbo.Products p ON p.Id = s.ProductId;

    DROP TABLE dbo.Sales;
    EXEC sp_rename N'dbo.Sales_New', N'Sales';
    CREATE INDEX IX_Sales_CreatedByUserId ON dbo.Sales(CreatedByUserId);
    CREATE INDEX IX_Sales_Date ON dbo.Sales(Date);
    CREATE INDEX IX_SaleLines_SaleId ON dbo.SaleLines(SaleId);
    CREATE INDEX IX_SaleLines_ProductId ON dbo.SaleLines(ProductId);
END;

IF OBJECT_ID(N'dbo.InventoryMovements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryMovements (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ProductId UNIQUEIDENTIFIER NOT NULL,
        SaleId UNIQUEIDENTIFIER NULL,
        PurchaseId UNIQUEIDENTIFIER NULL,
        PerformedByUserId UNIQUEIDENTIFIER NOT NULL,
        QuantityChange INT NOT NULL,
        StockAfter INT NOT NULL,
        Type NVARCHAR(20) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_InventoryMovements_Products FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id),
        CONSTRAINT FK_InventoryMovements_Sales FOREIGN KEY(SaleId) REFERENCES dbo.Sales(Id),
        CONSTRAINT FK_InventoryMovements_Purchases FOREIGN KEY(PurchaseId) REFERENCES dbo.Purchases(Id),
        CONSTRAINT FK_InventoryMovements_Users FOREIGN KEY(PerformedByUserId) REFERENCES dbo.Users(Id)
    );
    CREATE INDEX IX_InventoryMovements_ProductId ON dbo.InventoryMovements(ProductId);
    CREATE INDEX IX_InventoryMovements_CreatedAt ON dbo.InventoryMovements(CreatedAt);
END;

COMMIT TRANSACTION;
