using Dapper;
using EliraFashionBotiqueWebAPI.Data;
using EliraFashionBotiqueWebAPI.Models.DTOs;
using EliraFashionBotiqueWebAPI.Services.Interfaces;

namespace EliraFashionBotiqueWebAPI.Services
{
    public class ReportService : IReportService
    {
        private readonly DapperContext _context;

        public ReportService(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SalesReportDto>> GetSalesReportAsync(SalesReportFilter filter)
        {
            var query = @"
                SELECT 
                    O.OrderId,
                    O.OrderDate,
                    O.Status,
                    ISNULL(C.FullName, O.CustomerName) AS CustomerName,
                    P.ProductName,
                    CAT.CategoryName,
                    SC.SubcategoryName,
                    OI.Quantity,
                    OI.UnitPrice,
                    OI.DiscountedAmount,
                    OI.Subtotal,
                    O.TotalAmount,
                    O.FinalAmount
                FROM Orders O
                INNER JOIN Order_Item OI ON O.OrderId = OI.OrderId
                INNER JOIN Product_Variants PV ON OI.VariantId = PV.VariantId
                INNER JOIN Product P ON PV.ProductId = P.ProductId
                INNER JOIN Sub_Categories SC ON P.SubCategoryId = SC.SubCategoryId
                INNER JOIN Categories CAT ON SC.CategoryId = CAT.CategoryId
                LEFT JOIN Customer C ON O.CustomerId = C.CustomerId
                WHERE O.OrderDate BETWEEN @StartDate AND @EndDate
                    AND (@CategoryId IS NULL OR CAT.CategoryId = @CategoryId)
                    AND (@Status IS NULL OR O.Status = @Status)
                ORDER BY O.OrderDate DESC;";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<SalesReportDto>(query, filter);
        }
        public async Task<IEnumerable<LowStockReportDto>> GetLowStockReportAsync(LowStockFilter filter)
        {
            var query = @"
        SELECT 
            P.ProductName,
            PV.VariantSKU,
            SZ.SizeName,
            COL.ColorName,
            I.QuantityAvailable,
            I.ReorderLevel,
            (I.ReorderLevel - I.QuantityAvailable) AS UnitsNeeded,
            SC.SubcategoryName
        FROM Inventory I
        INNER JOIN Product_Variants PV ON I.VariantId = PV.VariantId
        INNER JOIN Product P ON PV.ProductId = P.ProductId
        INNER JOIN Sub_Categories SC ON P.SubCategoryId = SC.SubCategoryId
        LEFT JOIN Sizes SZ ON PV.SizeId = SZ.SizeId
        LEFT JOIN Color COL ON PV.ColorId = COL.ColorId
        WHERE I.QuantityAvailable <= I.ReorderLevel
            AND (@SubCategoryId IS NULL OR SC.SubCategoryId = @SubCategoryId)
        ORDER BY UnitsNeeded DESC;";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<LowStockReportDto>(query, filter);
        }

        public async Task<IEnumerable<CustomerOrderHistoryDto>> GetCustomerOrderHistoryAsync(CustomerHistoryFilter filter)
        {
            var query = @"
        SELECT 
            C.CustomerId,
            C.FullName,
            U.Email,
            C.PhoneNo,
            COUNT(O.OrderId) AS TotalOrders,
            SUM(O.FinalAmount) AS TotalSpent,
            MAX(O.OrderDate) AS LastOrderDate
        FROM Customer C
        INNER JOIN Users U ON C.UserId = U.UserId
        LEFT JOIN Orders O ON C.CustomerId = O.CustomerId
            AND (@StartDate IS NULL OR O.OrderDate >= @StartDate)
            AND (@EndDate IS NULL OR O.OrderDate <= @EndDate)
        WHERE (@CustomerId IS NULL OR C.CustomerId = @CustomerId)
        GROUP BY C.CustomerId, C.FullName, U.Email, C.PhoneNo
        ORDER BY TotalSpent DESC;";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<CustomerOrderHistoryDto>(query, filter);
        }
        public async Task<IEnumerable<ReturnRefundReportDto>> GetReturnRefundReportAsync(ReturnRefundFilter filter)
        {
            var query = @"
        SELECT 
            RO.ReturnId,
            RO.OrderId,
            RO.ReturnDate,
            RO.ReturnStatus,
            P.ProductName,
            PV.VariantSKU,
            RI.QuantityReturned,
            RI.ReturnedCondition,
            RI.ResolutionType,
            RF.RefundAmount,
            RF.RefundMethod,
            RF.RefundStatus
        FROM Return_Order RO
        INNER JOIN Return_Item RI ON RO.ReturnId = RI.ReturnId
        INNER JOIN Order_Item OI ON RI.OrderItemId = OI.OrderItemId
        INNER JOIN Product_Variants PV ON OI.VariantId = PV.VariantId
        INNER JOIN Product P ON PV.ProductId = P.ProductId
        LEFT JOIN Refund RF ON RF.ReturnId = RO.ReturnId
        WHERE (@StartDate IS NULL OR RO.ReturnDate >= @StartDate)
            AND (@EndDate IS NULL OR RO.ReturnDate <= @EndDate)
            AND (@ReturnStatus IS NULL OR RO.ReturnStatus = @ReturnStatus)
        ORDER BY RO.ReturnDate DESC;";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<ReturnRefundReportDto>(query, filter);
        }

        public async Task<IEnumerable<ProductPerformanceDto>> GetProductPerformanceReportAsync(ProductPerformanceFilter filter)
        {
            var query = @"
        SELECT 
            P.ProductId,
            P.ProductName,
            SC.SubcategoryName,
            SUM(OI.Quantity) AS TotalUnitsSold,
            SUM(OI.Subtotal) AS TotalRevenue,
            COUNT(DISTINCT O.OrderId) AS NumberOfOrders
        FROM Product P
        INNER JOIN Product_Variants PV ON P.ProductId = PV.ProductId
        INNER JOIN Order_Item OI ON PV.VariantId = OI.VariantId
        INNER JOIN Orders O ON OI.OrderId = O.OrderId
        INNER JOIN Sub_Categories SC ON P.SubCategoryId = SC.SubCategoryId
        WHERE (@StartDate IS NULL OR O.OrderDate >= @StartDate)
            AND (@EndDate IS NULL OR O.OrderDate <= @EndDate)
        GROUP BY P.ProductId, P.ProductName, SC.SubcategoryName
        ORDER BY TotalRevenue DESC;";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<ProductPerformanceDto>(query, filter);
        }

    }
}