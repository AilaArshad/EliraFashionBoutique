SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Ensure categories and subcategories exist
IF NOT EXISTS (SELECT * FROM Categories WHERE CategoryId = 1)
BEGIN
    SET IDENTITY_INSERT Categories ON;
    INSERT INTO Categories (CategoryId, CategoryName, Description) VALUES (1, 'Luxury Wear', 'Premium Boutique Collection');
    SET IDENTITY_INSERT Categories OFF;
END

-- Ensure subcategories exist
IF NOT EXISTS (SELECT * FROM Sub_Categories WHERE SubCategoryId = 1)
BEGIN
    SET IDENTITY_INSERT Sub_Categories ON;
    INSERT INTO Sub_Categories (SubCategoryId, CategoryId, SubcategoryName, SeasonType, IsActive) VALUES (1, 1, 'Festive Organza', 'Summer 2026', 1);
    SET IDENTITY_INSERT Sub_Categories OFF;
END

-- Ensure products exist
IF NOT EXISTS (SELECT * FROM Product WHERE ProductId = 1)
BEGIN
    SET IDENTITY_INSERT Product ON;
    INSERT INTO Product (ProductId, ProductName, Description, BasePrice, SKU, SubCategoryId, IsActive) 
    VALUES (1, 'Organza Luxury Suit', 'Embroidery details luxury suit', 6500.00, 'ORG-LUX-04', 1, 1);
    SET IDENTITY_INSERT Product OFF;
END

IF NOT EXISTS (SELECT * FROM Product WHERE ProductId = 2)
BEGIN
    SET IDENTITY_INSERT Product ON;
    INSERT INTO Product (ProductId, ProductName, Description, BasePrice, SKU, SubCategoryId, IsActive) 
    VALUES (2, 'Pret Linen Top', 'High-quality pret linen top', 4200.00, 'LIN-PRT-12', 1, 1);
    SET IDENTITY_INSERT Product OFF;
END

-- Ensure sizes exist
IF NOT EXISTS (SELECT * FROM Sizes WHERE SizeId = 1)
BEGIN
    SET IDENTITY_INSERT Sizes ON;
    INSERT INTO Sizes (SizeId, SizeName) VALUES (1, 'Medium');
    SET IDENTITY_INSERT Sizes OFF;
END

-- Ensure colors exist
IF NOT EXISTS (SELECT * FROM Color WHERE ColorId = 1)
BEGIN
    SET IDENTITY_INSERT Color ON;
    INSERT INTO Color (ColorId, ColorName, HexCode) VALUES (1, 'Black', '#000000');
    SET IDENTITY_INSERT Color OFF;
END

-- Ensure product variants exist
IF NOT EXISTS (SELECT * FROM Product_Variants WHERE VariantId = 1)
BEGIN
    SET IDENTITY_INSERT Product_Variants ON;
    INSERT INTO Product_Variants (VariantId, ProductId, SizeId, ColorId, VariantPrice, Weight, VariantSKU, IsActive)
    VALUES (1, 1, 1, 1, 6500.00, 0.5, 'ORG-LUX-04', 1);
    SET IDENTITY_INSERT Product_Variants OFF;
END

IF NOT EXISTS (SELECT * FROM Product_Variants WHERE VariantId = 2)
BEGIN
    SET IDENTITY_INSERT Product_Variants ON;
    INSERT INTO Product_Variants (VariantId, ProductId, SizeId, ColorId, VariantPrice, Weight, VariantSKU, IsActive)
    VALUES (2, 2, 1, 1, 4200.00, 0.3, 'LIN-PRT-12', 1);
    SET IDENTITY_INSERT Product_Variants OFF;
END

-- Ensure inventory exists
IF NOT EXISTS (SELECT * FROM Inventory WHERE VariantId = 1)
BEGIN
    INSERT INTO Inventory (VariantId, QuantityAvailable) VALUES (1, 10);
END

IF NOT EXISTS (SELECT * FROM Inventory WHERE VariantId = 2)
BEGIN
    INSERT INTO Inventory (VariantId, QuantityAvailable) VALUES (2, 15);
END

-- Ensure orders exist (Sana Malik and Amna Imran)
IF NOT EXISTS (SELECT * FROM Orders WHERE OrderId = 9901)
BEGIN
    SET IDENTITY_INSERT Orders ON;
    INSERT INTO Orders (OrderId, CustomerName, GuestEmail, GuestPhoneNo, OrderDate, Status, TotalAmount, DiscountedAmount, FinalAmount, ShippingAddress)
    VALUES (9901, 'Sana Malik', 'sana@example.com', '0300-1234567', '2026-06-01', 'Shipped', 6500.00, 0.00, 6500.00, 'House 12, Street 3, F-8, Islamabad');
    SET IDENTITY_INSERT Orders OFF;
END

IF NOT EXISTS (SELECT * FROM Orders WHERE OrderId = 9902)
BEGIN
    SET IDENTITY_INSERT Orders ON;
    INSERT INTO Orders (OrderId, CustomerName, GuestEmail, GuestPhoneNo, OrderDate, Status, TotalAmount, DiscountedAmount, FinalAmount, ShippingAddress)
    VALUES (9902, 'Amna Imran', 'amna@example.com', '0321-7654321', '2026-06-03', 'Shipped', 4200.00, 0.00, 4200.00, 'Apartment 4B, Askari 11, Lahore');
    SET IDENTITY_INSERT Orders OFF;
END

-- Ensure order items exist
IF NOT EXISTS (SELECT * FROM Order_Item WHERE OrderItemId = 9901)
BEGIN
    SET IDENTITY_INSERT Order_Item ON;
    INSERT INTO Order_Item (OrderItemId, OrderId, VariantId, Quantity, UnitPrice, DiscountedAmount, Subtotal)
    VALUES (9901, 9901, 1, 1, 6500.00, 0.00, 6500.00);
    SET IDENTITY_INSERT Order_Item OFF;
END

IF NOT EXISTS (SELECT * FROM Order_Item WHERE OrderItemId = 9902)
BEGIN
    SET IDENTITY_INSERT Order_Item ON;
    INSERT INTO Order_Item (OrderItemId, OrderId, VariantId, Quantity, UnitPrice, DiscountedAmount, Subtotal)
    VALUES (9902, 9902, 2, 1, 4200.00, 0.00, 4200.00);
    SET IDENTITY_INSERT Order_Item OFF;
END

-- Ensure payments exist
IF NOT EXISTS (SELECT * FROM Payment WHERE PaymentId = 9901)
BEGIN
    SET IDENTITY_INSERT Payment ON;
    INSERT INTO Payment (PaymentId, OrderId, Amount, PaymentStatus, PaidAt)
    VALUES (9901, 9901, 6500.00, 'Paid', '2026-06-01');
    SET IDENTITY_INSERT Payment OFF;
END

IF NOT EXISTS (SELECT * FROM Payment WHERE PaymentId = 9902)
BEGIN
    SET IDENTITY_INSERT Payment ON;
    INSERT INTO Payment (PaymentId, OrderId, Amount, PaymentStatus, PaidAt)
    VALUES (9902, 9902, 4200.00, 'Paid', '2026-06-03');
    SET IDENTITY_INSERT Payment OFF;
END

-- Ensure return orders exist for testing
IF NOT EXISTS (SELECT * FROM Return_Order WHERE ReturnId = 1)
BEGIN
    SET IDENTITY_INSERT Return_Order ON;
    INSERT INTO Return_Order (ReturnId, OrderId, ReturnDate, ReturnStatus, CustomReasonText, ProcessedBy)
    VALUES (1, 9901, '2026-06-02', 'Pending', 'Embroidery thread coming out from sleeve', NULL);
    SET IDENTITY_INSERT Return_Order OFF;
END

IF NOT EXISTS (SELECT * FROM Return_Order WHERE ReturnId = 2)
BEGIN
    SET IDENTITY_INSERT Return_Order ON;
    INSERT INTO Return_Order (ReturnId, OrderId, ReturnDate, ReturnStatus, CustomReasonText, ProcessedBy)
    VALUES (2, 9902, '2026-06-05', 'Approved', 'Fabric color is fading out near border', NULL);
    SET IDENTITY_INSERT Return_Order OFF;
END

-- Ensure return items exist
IF NOT EXISTS (SELECT * FROM Return_Item WHERE ReturnItemId = 1)
BEGIN
    SET IDENTITY_INSERT Return_Item ON;
    INSERT INTO Return_Item (ReturnItemId, ReturnId, OrderItemId, QuantityReturned, ReturnedCondition, ResolutionType)
    VALUES (1, 1, 9901, 1, 'Damaged', 'Full Refund to Bank');
    SET IDENTITY_INSERT Return_Item OFF;
END

IF NOT EXISTS (SELECT * FROM Return_Item WHERE ReturnItemId = 2)
BEGIN
    SET IDENTITY_INSERT Return_Item ON;
    INSERT INTO Return_Item (ReturnItemId, ReturnId, OrderItemId, QuantityReturned, ReturnedCondition, ResolutionType)
    VALUES (2, 2, 9902, 1, 'Damaged', 'Full Refund to Bank');
    SET IDENTITY_INSERT Return_Item OFF;
END

-- Ensure refunds exist
IF NOT EXISTS (SELECT * FROM Refund WHERE RefundId = 1)
BEGIN
    SET IDENTITY_INSERT Refund ON;
    INSERT INTO Refund (RefundId, ReturnId, PaymentId, RefundAmount, RefundMethod, RefundStatus, RefundDate, BankName, BankAccountNumber)
    VALUES (1, 1, 9901, 6500.00, 'Bank Transfer', 'Escrow Hold', NULL, 'Meezan Bank', 'PK12MEZN0000123456789');
    SET IDENTITY_INSERT Refund OFF;
END

IF NOT EXISTS (SELECT * FROM Refund WHERE RefundId = 2)
BEGIN
    SET IDENTITY_INSERT Refund ON;
    INSERT INTO Refund (RefundId, ReturnId, PaymentId, RefundAmount, RefundMethod, RefundStatus, RefundDate, BankName, BankAccountNumber)
    VALUES (2, 2, 9902, 4200.00, 'Bank Transfer', 'Processing Settlement', '2026-06-05', 'Habib Bank Limited (HBL)', 'PK56HABB0000987654321');
    SET IDENTITY_INSERT Refund OFF;
END
