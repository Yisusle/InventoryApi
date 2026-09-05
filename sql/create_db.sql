IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'InventoryDb')
BEGIN
    CREATE DATABASE [InventoryDb];
END
GO

IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = N'inventory_app')
BEGIN
    CREATE LOGIN [inventory_app] WITH PASSWORD = N'ReplaceWithStrongP@ssw0rd!';
END
GO

USE [InventoryDb];
GO

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = N'inventory_app')
BEGIN
    CREATE USER [inventory_app] FOR LOGIN [inventory_app];
    ALTER ROLE db_owner ADD MEMBER [inventory_app];
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE dbo.Users (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        Email NVARCHAR(200) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(500) NOT NULL,
        Role NVARCHAR(50) NOT NULL DEFAULT 'User',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_Users_Username ON dbo.Users(Username);
    CREATE INDEX IX_Users_Email ON dbo.Users(Email);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
BEGIN
    CREATE TABLE dbo.Categories (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL UNIQUE,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_Categories_Name ON dbo.Categories(Name);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
BEGIN
    CREATE TABLE dbo.Products (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Sku NVARCHAR(100) NULL,
        CategoryId UNIQUEIDENTIFIER NULL,
        Price DECIMAL(18,2) NOT NULL,
        Stock INT NOT NULL DEFAULT 0,
        MinimumStock INT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Products_Categories FOREIGN KEY(CategoryId) REFERENCES dbo.Categories(Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);
    CREATE UNIQUE INDEX IX_Products_Sku_Unique ON dbo.Products(Sku) WHERE Sku IS NOT NULL;
    CREATE INDEX IX_Products_Name ON dbo.Products(Name);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Purchases')
BEGIN
    CREATE TABLE dbo.Purchases (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Date DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ProductId UNIQUEIDENTIFIER NOT NULL,
        Quantity INT NOT NULL,
        TotalCost DECIMAL(18,2) NOT NULL,
        CreatedByUserId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Purchases_Products FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id),
        CONSTRAINT FK_Purchases_Users FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id)
    );
    CREATE INDEX IX_Purchases_ProductId ON dbo.Purchases(ProductId);
    CREATE INDEX IX_Purchases_CreatedByUserId ON dbo.Purchases(CreatedByUserId);
    CREATE INDEX IX_Purchases_Date ON dbo.Purchases(Date);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sales')
BEGIN
    CREATE TABLE dbo.Sales (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Date DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedByUserId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Sales_Users FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id)
    );
    CREATE INDEX IX_Sales_CreatedByUserId ON dbo.Sales(CreatedByUserId);
    CREATE INDEX IX_Sales_Date ON dbo.Sales(Date);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SaleLines')
BEGIN
    CREATE TABLE dbo.SaleLines (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        SaleId UNIQUEIDENTIFIER NOT NULL,
        ProductId UNIQUEIDENTIFIER NOT NULL,
        ProductName NVARCHAR(200) NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_SaleLines_Sales FOREIGN KEY(SaleId) REFERENCES dbo.Sales(Id) ON DELETE CASCADE,
        CONSTRAINT FK_SaleLines_Products FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id)
    );
    CREATE INDEX IX_SaleLines_SaleId ON dbo.SaleLines(SaleId);
    CREATE INDEX IX_SaleLines_ProductId ON dbo.SaleLines(ProductId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryMovements')
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
        Reason NVARCHAR(500) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_InventoryMovements_Products FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id),
        CONSTRAINT FK_InventoryMovements_Sales FOREIGN KEY(SaleId) REFERENCES dbo.Sales(Id),
        CONSTRAINT FK_InventoryMovements_Purchases FOREIGN KEY(PurchaseId) REFERENCES dbo.Purchases(Id),
        CONSTRAINT FK_InventoryMovements_Users FOREIGN KEY(PerformedByUserId) REFERENCES dbo.Users(Id)
    );
    CREATE INDEX IX_InventoryMovements_ProductId ON dbo.InventoryMovements(ProductId);
    CREATE INDEX IX_InventoryMovements_CreatedAt ON dbo.InventoryMovements(CreatedAt);
END
GO

PRINT '';
PRINT '========================================';
PRINT 'Database Creation Complete!';
PRINT '========================================';
PRINT 'Tables created: Users, Categories, Products, Purchases, Sales';
PRINT 'All tables include appropriate indexes and foreign keys.';
PRINT 'Admin user can be inserted manually with BCrypt hashed password.';
PRINT '========================================';
PRINT '';
