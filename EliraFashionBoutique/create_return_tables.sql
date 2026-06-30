IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Return_Order')
BEGIN
    CREATE TABLE Return_Order (
        ReturnId INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        ReturnDate DATETIME DEFAULT GETDATE(),
        ReturnStatus VARCHAR(50) DEFAULT 'Pending',
        CustomReasonText VARCHAR(MAX),
        ProcessedBy INT NULL,
        FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Return_Item')
BEGIN
    CREATE TABLE Return_Item (
        ReturnItemId INT IDENTITY(1,1) PRIMARY KEY,
        ReturnId INT NOT NULL,
        OrderItemId INT NOT NULL,
        QuantityReturned INT NOT NULL,
        ReturnedCondition VARCHAR(100),
        ResolutionType VARCHAR(100) DEFAULT 'Full Refund to Bank', 
        FOREIGN KEY (ReturnId) REFERENCES Return_Order(ReturnId) ON DELETE CASCADE,
        FOREIGN KEY (OrderItemId) REFERENCES Order_Item(OrderItemId)
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Refund')
BEGIN
    CREATE TABLE Refund (
        RefundId INT IDENTITY(1,1) PRIMARY KEY,
        ReturnId INT NOT NULL,
        PaymentId INT NULL,
        RefundAmount DECIMAL(10, 2) NOT NULL,
        RefundMethod VARCHAR(50) DEFAULT 'Bank Transfer',
        RefundStatus VARCHAR(50),
        RefundDate DATE NULL,
        BankName VARCHAR(100),
        BankAccountNumber VARCHAR(50),
        FOREIGN KEY (ReturnId) REFERENCES Return_Order(ReturnId),
        FOREIGN KEY (PaymentId) REFERENCES Payment(PaymentId)
    );
END
