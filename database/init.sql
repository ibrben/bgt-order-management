IF DB_ID(N'OrderManagement') IS NULL
BEGIN
    CREATE DATABASE OrderManagement;
END;
GO

USE OrderManagement;
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        CustomerId int NOT NULL PRIMARY KEY,
        CustomerName nvarchar(200) NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        ProductId int NOT NULL PRIMARY KEY,
        ProductName nvarchar(200) NOT NULL,
        Price decimal(18, 2) NOT NULL,
        StockQuantity int NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1)
    );
END;
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        OrderId bigint IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        CustomerId int NOT NULL,
        OrderStatus varchar(30) NOT NULL,
        TotalAmount decimal(18, 2) NOT NULL,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NOT NULL,
        CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId)
    );
END;
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems
    (
        OrderItemId bigint IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        OrderId bigint NOT NULL,
        ProductId int NOT NULL,
        Quantity int NOT NULL,
        UnitPrice decimal(18, 2) NOT NULL,
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId),
        CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
    );
END;
GO

INSERT INTO dbo.Customers (CustomerId, CustomerName)
SELECT seed.CustomerId, seed.CustomerName
FROM (VALUES
    (101, N'Customer 101'),
    (102, N'Customer 102'),
    (103, N'Customer 103')
) AS seed(CustomerId, CustomerName)
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Customers AS customer
    WHERE customer.CustomerId = seed.CustomerId
);
GO

INSERT INTO dbo.Products (ProductId, ProductName, Price, StockQuantity, IsActive)
SELECT seed.ProductId, seed.ProductName, seed.Price, seed.StockQuantity, 1
FROM (VALUES
    (1010, N'Honda wave', CAST(20000 AS decimal(18, 2)), 10),
    (1002, N'Honda click', CAST(15000 AS decimal(18, 2)), 10),
    (1003, N'Yamaha mio', CAST(10000 AS decimal(18, 2)), 10)
) AS seed(ProductId, ProductName, Price, StockQuantity)
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Products AS product
    WHERE product.ProductId = seed.ProductId
);
GO