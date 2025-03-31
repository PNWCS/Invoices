// Mappers/InvoiceMapper.cs
using QB_Invoices_Lib;
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
                TimeCreated = invoice.TimeCreated?.GetValue(),
                TimeModified = invoice.TimeModified?.GetValue(),
                EditSequence = invoice.EditSequence?.GetValue(),
                TxnNumber = invoice.TxnNumber?.GetValue(),
                CustomerName = invoice.CustomerRef?.FullName?.GetValue(),
                ClassRef = invoice.ClassRef?.ListID?.GetValue(),
                ARAccountRef = invoice.ARAccountRef?.ListID?.GetValue(),
                TemplateRef = invoice.TemplateRef?.ListID?.GetValue(),
                TxnDate = invoice.TxnDate?.GetValue(),
                RefNumber = invoice.RefNumber?.GetValue(),
                BillAddress = invoice.BillAddress?.Addr1?.GetValue(), // Adjust as needed
                ShipAddress = invoice.ShipAddress?.Addr1?.GetValue(), // Adjust as needed
                IsPending = invoice.IsPending?.GetValue(),
                IsFinanceCharge = invoice.IsFinanceCharge?.GetValue(),
                PONumber = invoice.PONumber?.GetValue(),
                TermsRef = invoice.TermsRef?.ListID?.GetValue(),
                DueDate = invoice.DueDate?.GetValue(),
                SalesRepRef = invoice.SalesRepRef?.ListID?.GetValue(),
                FOB = invoice.FOB?.GetValue(),
                ShipDate = invoice.ShipDate?.GetValue(),
                ShipMethodRef = invoice.ShipMethodRef?.ListID?.GetValue(),
                Subtotal = (decimal?)(invoice.Subtotal?.GetValue()),
                // ItemSalesTaxRef = invoice.ItemSalesTaxRef?.GetValue(),
                SalesTaxPercentage = (decimal?)invoice.SalesTaxPercentage?.GetValue(),
                SalesTaxTotal = (decimal?)invoice.SalesTaxTotal?.GetValue(),
                AppliedAmount = (decimal?)invoice.AppliedAmount?.GetValue(),
                BalanceRemaining = (decimal?)invoice.BalanceRemaining?.GetValue(),
                CurrencyRef = invoice.CurrencyRef?.ListID?.GetValue(),
                ExchangeRate = invoice.ExchangeRate?.GetValue(),
                BalanceRemainingInHomeCurrency = (decimal?)invoice.BalanceRemainingInHomeCurrency?.GetValue(),
                CompanyID = invoice.Memo?.GetValue(),
                IsPaid = invoice.IsPaid?.GetValue(),
                CustomerMsgRef = invoice.CustomerMsgRef?.ListID?.GetValue(),
                IsToBePrinted = invoice.IsToBePrinted?.GetValue(),
                IsToBeEmailed = invoice.IsToBeEmailed?.GetValue(),
                IsTaxIncluded = invoice.IsTaxIncluded?.GetValue(),
                CustomerSalesTaxCodeRef = invoice.CustomerSalesTaxCodeRef?.ListID?.GetValue(),
                SuggestedDiscountAmount = (decimal?)invoice.SuggestedDiscountAmount?.GetValue(),
                SuggestedDiscountDate = invoice.SuggestedDiscountDate?.GetValue(),
                Other = invoice.Other?.GetValue(),
                ExternalGUID = invoice.ExternalGUID?.GetValue()
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
