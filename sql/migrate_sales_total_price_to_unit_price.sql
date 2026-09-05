SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.Sales', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.Sales', N'UnitPrice') IS NULL
    AND COL_LENGTH(N'dbo.Sales', N'TotalPrice') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Sales ADD UnitPrice DECIMAL(18,2) NULL;

    UPDATE dbo.Sales
    SET UnitPrice = ROUND(TotalPrice / NULLIF(Quantity, 0), 2);

    IF EXISTS (SELECT 1 FROM dbo.Sales WHERE UnitPrice IS NULL)
        THROW 50000, 'No se pudo migrar una venta con cantidad inválida.', 1;

    ALTER TABLE dbo.Sales ALTER COLUMN UnitPrice DECIMAL(18,2) NOT NULL;
    ALTER TABLE dbo.Sales DROP COLUMN TotalPrice;
END;

COMMIT TRANSACTION;
