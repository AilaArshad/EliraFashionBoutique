using ClosedXML.Excel;
using QuestPDF.Fluent;
using EliraFashionBotiqueWebAPI.Models.DTOs;
using EliraFashionBotiqueWebAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EliraFashionBotiqueWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("sales")]
        public async Task<IActionResult> GetSalesReport([FromQuery] SalesReportFilter filter)
        {
            var result = await _reportService.GetSalesReportAsync(filter);
            return Ok(result);
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStockReport([FromQuery] LowStockFilter filter)
        {
            var result = await _reportService.GetLowStockReportAsync(filter);
            return Ok(result);
        }

        [HttpGet("customer-history")]
        public async Task<IActionResult> GetCustomerOrderHistory([FromQuery] CustomerHistoryFilter filter)
        {
            var result = await _reportService.GetCustomerOrderHistoryAsync(filter);
            return Ok(result);
        }

        [HttpGet("returns")]
        public async Task<IActionResult> GetReturnRefundReport([FromQuery] ReturnRefundFilter filter)
        {
            var result = await _reportService.GetReturnRefundReportAsync(filter);
            return Ok(result);
        }

        [HttpGet("product-performance")]
        public async Task<IActionResult> GetProductPerformanceReport([FromQuery] ProductPerformanceFilter filter)
        {
            var result = await _reportService.GetProductPerformanceReportAsync(filter);
            return Ok(result);
        }


        [HttpGet("customer-history/export/excel")]
        public async Task<IActionResult> ExportCustomerHistoryExcel([FromQuery] CustomerHistoryFilter filter)
        {
            var data = await _reportService.GetCustomerOrderHistoryAsync(filter);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Customer Report");

            worksheet.Cell(1, 1).Value = "Customer ID";
            worksheet.Cell(1, 2).Value = "Full Name";
            worksheet.Cell(1, 3).Value = "Email";
            worksheet.Cell(1, 4).Value = "Phone No";
            worksheet.Cell(1, 5).Value = "Total Orders";
            worksheet.Cell(1, 6).Value = "Total Spent";
            worksheet.Cell(1, 7).Value = "Last Order Date";

            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.CustomerId;
                worksheet.Cell(row, 2).Value = item.FullName;
                worksheet.Cell(row, 3).Value = item.Email;
                worksheet.Cell(row, 4).Value = item.PhoneNo;
                worksheet.Cell(row, 5).Value = item.TotalOrders;
                worksheet.Cell(row, 6).Value = item.TotalSpent ?? 0;
                worksheet.Cell(row, 7).Value = item.LastOrderDate?.ToString("yyyy-MM-dd") ?? "";
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "customer-report.xlsx");
        }

        [HttpGet("customer-history/export/pdf")]
        public async Task<IActionResult> ExportCustomerHistoryPdf([FromQuery] CustomerHistoryFilter filter)
        {
            var data = await _reportService.GetCustomerOrderHistoryAsync(filter);

            var pdfBytes = QuestPDF.Fluent.Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text("Customer Report").FontSize(18).Bold();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Customer ID");
                            header.Cell().Text("Full Name");
                            header.Cell().Text("Email");
                            header.Cell().Text("Total Orders");
                            header.Cell().Text("Total Spent");
                            header.Cell().Text("Last Order");
                        });

                        foreach (var item in data)
                        {
                            table.Cell().Text(item.CustomerId.ToString());
                            table.Cell().Text(item.FullName);
                            table.Cell().Text(item.Email);
                            table.Cell().Text(item.TotalOrders.ToString());
                            table.Cell().Text((item.TotalSpent ?? 0).ToString("C"));
                            table.Cell().Text(item.LastOrderDate?.ToString("yyyy-MM-dd") ?? "-");
                        }
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", "customer-report.pdf");
        }

        [HttpGet("sales/export/excel")]
        public async Task<IActionResult> ExportSalesExcel([FromQuery] SalesReportFilter filter)
        {
            var data = await _reportService.GetSalesReportAsync(filter);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sales Report");

            worksheet.Cell(1, 1).Value = "Order ID";
            worksheet.Cell(1, 2).Value = "Order Date";
            worksheet.Cell(1, 3).Value = "Status";
            worksheet.Cell(1, 4).Value = "Customer";
            worksheet.Cell(1, 5).Value = "Product";
            worksheet.Cell(1, 6).Value = "Category";
            worksheet.Cell(1, 7).Value = "Subcategory";
            worksheet.Cell(1, 8).Value = "Quantity";
            worksheet.Cell(1, 9).Value = "Unit Price";
            worksheet.Cell(1, 10).Value = "Discounted Amount";
            worksheet.Cell(1, 11).Value = "Subtotal";
            worksheet.Cell(1, 12).Value = "Total Amount";
            worksheet.Cell(1, 13).Value = "Final Amount";

            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.OrderId;
                worksheet.Cell(row, 2).Value = item.OrderDate.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 3).Value = item.Status;
                worksheet.Cell(row, 4).Value = item.CustomerName;
                worksheet.Cell(row, 5).Value = item.ProductName;
                worksheet.Cell(row, 6).Value = item.CategoryName;
                worksheet.Cell(row, 7).Value = item.SubcategoryName;
                worksheet.Cell(row, 8).Value = item.Quantity;
                worksheet.Cell(row, 9).Value = item.UnitPrice;
                worksheet.Cell(row, 10).Value = item.DiscountedAmount;
                worksheet.Cell(row, 11).Value = item.Subtotal;
                worksheet.Cell(row, 12).Value = item.TotalAmount;
                worksheet.Cell(row, 13).Value = item.FinalAmount;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "sales-report.xlsx");
        }

        [HttpGet("sales/export/pdf")]
        public async Task<IActionResult> ExportSalesPdf([FromQuery] SalesReportFilter filter)
        {
            var data = await _reportService.GetSalesReportAsync(filter);

            var pdfBytes = QuestPDF.Fluent.Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Margin(20);
                    page.Header().Text("Sales Report").FontSize(18).Bold();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Order ID");
                            header.Cell().Text("Date");
                            header.Cell().Text("Customer");
                            header.Cell().Text("Product");
                            header.Cell().Text("Qty");
                            header.Cell().Text("Final Amount");
                        });

                        foreach (var item in data)
                        {
                            table.Cell().Text(item.OrderId.ToString());
                            table.Cell().Text(item.OrderDate.ToString("yyyy-MM-dd"));
                            table.Cell().Text(item.CustomerName);
                            table.Cell().Text(item.ProductName);
                            table.Cell().Text(item.Quantity.ToString());
                            table.Cell().Text(item.FinalAmount.ToString("C"));
                        }
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", "sales-report.pdf");
        }

        // ===================== EXPORT: PRODUCT PERFORMANCE =====================

        [HttpGet("product-performance/export/excel")]
        public async Task<IActionResult> ExportProductPerformanceExcel([FromQuery] ProductPerformanceFilter filter)
        {
            var data = await _reportService.GetProductPerformanceReportAsync(filter);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Product Report");

            worksheet.Cell(1, 1).Value = "Product ID";
            worksheet.Cell(1, 2).Value = "Product Name";
            worksheet.Cell(1, 3).Value = "Subcategory";
            worksheet.Cell(1, 4).Value = "Total Units Sold";
            worksheet.Cell(1, 5).Value = "Total Revenue";
            worksheet.Cell(1, 6).Value = "Number Of Orders";

            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.ProductId;
                worksheet.Cell(row, 2).Value = item.ProductName;
                worksheet.Cell(row, 3).Value = item.SubcategoryName;
                worksheet.Cell(row, 4).Value = item.TotalUnitsSold;
                worksheet.Cell(row, 5).Value = item.TotalRevenue;
                worksheet.Cell(row, 6).Value = item.NumberOfOrders;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "product-report.xlsx");
        }

        [HttpGet("product-performance/export/pdf")]
        public async Task<IActionResult> ExportProductPerformancePdf([FromQuery] ProductPerformanceFilter filter)
        {
            var data = await _reportService.GetProductPerformanceReportAsync(filter);

            var pdfBytes = QuestPDF.Fluent.Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Margin(20);
                    page.Header().Text("Product Report").FontSize(18).Bold();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Product Name");
                            header.Cell().Text("Subcategory");
                            header.Cell().Text("Units Sold");
                            header.Cell().Text("Revenue");
                            header.Cell().Text("Orders");
                        });

                        foreach (var item in data)
                        {
                            table.Cell().Text(item.ProductName);
                            table.Cell().Text(item.SubcategoryName);
                            table.Cell().Text(item.TotalUnitsSold.ToString());
                            table.Cell().Text(item.TotalRevenue.ToString("C"));
                            table.Cell().Text(item.NumberOfOrders.ToString());
                        }
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", "product-report.pdf");
        }

        // ===================== EXPORT: LOW STOCK / INVENTORY =====================

        [HttpGet("low-stock/export/excel")]
        public async Task<IActionResult> ExportLowStockExcel([FromQuery] LowStockFilter filter)
        {
            var data = await _reportService.GetLowStockReportAsync(filter);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Inventory Report");

            worksheet.Cell(1, 1).Value = "Product Name";
            worksheet.Cell(1, 2).Value = "Variant SKU";
            worksheet.Cell(1, 3).Value = "Size";
            worksheet.Cell(1, 4).Value = "Color";
            worksheet.Cell(1, 5).Value = "Available";
            worksheet.Cell(1, 6).Value = "Reorder Level";
            worksheet.Cell(1, 7).Value = "Units Needed";
            worksheet.Cell(1, 8).Value = "Subcategory";

            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.ProductName;
                worksheet.Cell(row, 2).Value = item.VariantSKU;
                worksheet.Cell(row, 3).Value = item.SizeName;
                worksheet.Cell(row, 4).Value = item.ColorName;
                worksheet.Cell(row, 5).Value = item.QuantityAvailable;
                worksheet.Cell(row, 6).Value = item.ReorderLevel;
                worksheet.Cell(row, 7).Value = item.UnitsNeeded;
                worksheet.Cell(row, 8).Value = item.SubcategoryName;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "inventory-report.xlsx");
        }

        [HttpGet("low-stock/export/pdf")]
        public async Task<IActionResult> ExportLowStockPdf([FromQuery] LowStockFilter filter)
        {
            var data = await _reportService.GetLowStockReportAsync(filter);

            var pdfBytes = QuestPDF.Fluent.Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Margin(20);
                    page.Header().Text("Inventory Report").FontSize(18).Bold();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Product Name");
                            header.Cell().Text("SKU");
                            header.Cell().Text("Available");
                            header.Cell().Text("Reorder Level");
                            header.Cell().Text("Units Needed");
                        });

                        foreach (var item in data)
                        {
                            table.Cell().Text(item.ProductName);
                            table.Cell().Text(item.VariantSKU);
                            table.Cell().Text(item.QuantityAvailable.ToString());
                            table.Cell().Text(item.ReorderLevel.ToString());
                            table.Cell().Text(item.UnitsNeeded.ToString());
                        }
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", "inventory-report.pdf");
        }

        // ===================== EXPORT: RETURNS & REFUNDS =====================

        [HttpGet("returns/export/excel")]
        public async Task<IActionResult> ExportReturnsExcel([FromQuery] ReturnRefundFilter filter)
        {
            var data = await _reportService.GetReturnRefundReportAsync(filter);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Returns Report");

            worksheet.Cell(1, 1).Value = "Return ID";
            worksheet.Cell(1, 2).Value = "Order ID";
            worksheet.Cell(1, 3).Value = "Return Date";
            worksheet.Cell(1, 4).Value = "Return Status";
            worksheet.Cell(1, 5).Value = "Product";
            worksheet.Cell(1, 6).Value = "Variant SKU";
            worksheet.Cell(1, 7).Value = "Qty Returned";
            worksheet.Cell(1, 8).Value = "Condition";
            worksheet.Cell(1, 9).Value = "Resolution Type";
            worksheet.Cell(1, 10).Value = "Refund Amount";
            worksheet.Cell(1, 11).Value = "Refund Method";
            worksheet.Cell(1, 12).Value = "Refund Status";

            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.ReturnId;
                worksheet.Cell(row, 2).Value = item.OrderId;
                worksheet.Cell(row, 3).Value = item.ReturnDate.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 4).Value = item.ReturnStatus;
                worksheet.Cell(row, 5).Value = item.ProductName;
                worksheet.Cell(row, 6).Value = item.VariantSKU;
                worksheet.Cell(row, 7).Value = item.QuantityReturned;
                worksheet.Cell(row, 8).Value = item.ReturnedCondition;
                worksheet.Cell(row, 9).Value = item.ResolutionType;
                worksheet.Cell(row, 10).Value = item.RefundAmount ?? 0;
                worksheet.Cell(row, 11).Value = item.RefundMethod;
                worksheet.Cell(row, 12).Value = item.RefundStatus;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "returns-report.xlsx");
        }

        [HttpGet("returns/export/pdf")]
        public async Task<IActionResult> ExportReturnsPdf([FromQuery] ReturnRefundFilter filter)
        {
            var data = await _reportService.GetReturnRefundReportAsync(filter);

            var pdfBytes = QuestPDF.Fluent.Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Margin(20);
                    page.Header().Text("Returns & Refunds Report").FontSize(18).Bold();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Return ID");
                            header.Cell().Text("Order ID");
                            header.Cell().Text("Product");
                            header.Cell().Text("Qty");
                            header.Cell().Text("Refund Amount");
                            header.Cell().Text("Refund Status");
                        });

                        foreach (var item in data)
                        {
                            table.Cell().Text(item.ReturnId.ToString());
                            table.Cell().Text(item.OrderId.ToString());
                            table.Cell().Text(item.ProductName);
                            table.Cell().Text(item.QuantityReturned.ToString());
                            table.Cell().Text((item.RefundAmount ?? 0).ToString("C"));
                            table.Cell().Text(item.RefundStatus ?? "-");
                        }
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", "returns-report.pdf");
        }
    }
}