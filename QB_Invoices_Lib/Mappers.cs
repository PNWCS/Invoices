// Mappers/InvoiceMapper.cs
using QBFC16Lib; // Assuming QBFC16Lib is referenced for QuickBooks integration

namespace QB_Invoices_Lib
{
    public static class InvoiceMapper
    {
        public static Invoice MapToDto(IInvoiceRet invoice)
        {
            var dto = new Invoice
            {
                TxnID = invoice.TxnID?.GetValue(),
                CustomerName = invoice.CustomerRef?.FullName?.GetValue(),
                InvoiceDate = invoice.TxnDate?.GetValue(),
                InvoiceNumber = invoice.RefNumber?.GetValue(),
                InoviceAmount = (decimal?)(invoice.Subtotal?.GetValue()),
                BalanceRemaining = (decimal?)invoice.BalanceRemaining?.GetValue(),
                CompanyID = invoice.Memo?.GetValue(),
                Memo = invoice.Memo?.GetValue(),
            };
            // Map line items (parts)
            if (invoice.ORInvoiceLineRetList != null)
            {
                for (int i = 0; i < invoice.ORInvoiceLineRetList.Count; i++)
                {
                    var line = invoice.ORInvoiceLineRetList.GetAt(i);

                    var lineItem = new InvoiceLineItemDto
                    {
                        ItemName = line.InvoiceLineRet?.ItemRef?.FullName?.GetValue(),
                        Quantity = (int)line.InvoiceLineRet?.Quantity?.GetValue(), // Fixed line
                        ItemPrice = line.InvoiceLineRet?.ORRate?.Rate?.GetValue(), // Updated line
                        Amount = (decimal?)line.InvoiceLineRet?.Amount?.GetValue() // Fixed line
                    };
                    dto.LineItems.Add(lineItem);
                }
            }

            return dto;
        }
    }
}
