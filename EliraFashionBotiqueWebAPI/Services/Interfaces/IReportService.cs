using EliraFashionBotiqueWebAPI.Models.DTOs;

namespace EliraFashionBotiqueWebAPI.Services.Interfaces
{
    public interface IReportService
    {
        Task<IEnumerable<SalesReportDto>> GetSalesReportAsync(SalesReportFilter filter);
        Task<IEnumerable<LowStockReportDto>> GetLowStockReportAsync(LowStockFilter filter);
        Task<IEnumerable<CustomerOrderHistoryDto>> GetCustomerOrderHistoryAsync(CustomerHistoryFilter filter);
        Task<IEnumerable<ReturnRefundReportDto>> GetReturnRefundReportAsync(ReturnRefundFilter filter);
        Task<IEnumerable<ProductPerformanceDto>> GetProductPerformanceReportAsync(ProductPerformanceFilter filter);
    }
}