-- A Payment for the existing order
INSERT INTO Payment (PaymentId, OrderId, TransactionId, Amount, PaymentStatus) 
VALUES (1, 1, 'TXN-00123', 2900, 'Completed');

-- A Return for that order
INSERT INTO Return_Order (ReturnId, OrderId, ReturnDate, ReturnStatus, ProcessedBy) 
VALUES (1, 1, '2025-05-20', 'Approved', NULL);

-- The specific item returned
INSERT INTO Return_Item (ReturnItemId, ReturnId, OrderItemId, QuantityReturned, ReturnedCondition, ResolutionType) 
VALUES (1, 1, 1, 1, 'Unworn, tags attached', 'Refund');

-- The refund issued
INSERT INTO Refund (RefundId, ReturnId, PaymentId, RefundAmount, RefundMethod, RefundStatus, RefundDate) 
VALUES (1, 1, 1, 1450, 'Original Payment Method', 'Processed', '2025-05-22');