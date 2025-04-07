using System.Text;

namespace QB_Invoices_Lib
{
    public class Invoice
    {

        public string? TxnID { get; set; }
        public DateTime? TimeCreated { get; set; }
        public DateTime? TimeModified { get; set; }
        public string? EditSequence { get; set; }
        public int? TxnNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? ClassRef { get; set; }
        public string? ARAccountRef { get; set; }
        public string? TemplateRef { get; set; }
        public DateTime? TxnDate { get; set; }
        public string? RefNumber { get; set; }
        public string? BillAddress { get; set; }
        public string? ShipAddress { get; set; }
        public bool? IsPending { get; set; }
        public bool? IsFinanceCharge { get; set; }
        public string? PONumber { get; set; }
        public string? Memo { get; set; }
        public string? TermsRef { get; set; }
        public DateTime? DueDate { get; set; }
        public string? SalesRepRef { get; set; }
        public string? FOB { get; set; }
        public DateTime? ShipDate { get; set; }
        public string? ShipMethodRef { get; set; }
        public decimal? Subtotal { get; set; }
        public string? ItemSalesTaxRef { get; set; }
        public decimal? SalesTaxPercentage { get; set; }
        public decimal? SalesTaxTotal { get; set; }
        public decimal? AppliedAmount { get; set; }
        public decimal? BalanceRemaining { get; set; }
        public string? CurrencyRef { get; set; }
        public float? ExchangeRate { get; set; }
        public decimal? BalanceRemainingInHomeCurrency { get; set; }
        public string? CompanyID { get; set; }
        public bool? IsPaid { get; set; }
        public string? CustomerMsgRef { get; set; }
        public bool? IsToBePrinted { get; set; }
        public bool? IsToBeEmailed { get; set; }
        public bool? IsTaxIncluded { get; set; }
        public string? CustomerSalesTaxCodeRef { get; set; }
        public decimal? SuggestedDiscountAmount { get; set; }
        public DateTime? SuggestedDiscountDate { get; set; }
        public string? Other { get; set; }
        public string? ExternalGUID { get; set; }
        public List<InvoiceLineItemDto> LineItems { get; set; } = new List<InvoiceLineItemDto>(); // New property for parts

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Invoice TxnID: {TxnID}");
            sb.AppendLine($"Customer Name: {CustomerName}");
            sb.AppendLine($"Invoice Date: {TxnDate?.ToShortDateString()}");
            sb.AppendLine($"Invoice Number: {RefNumber}");
            sb.AppendLine($"Total Amount: {Subtotal:C}");
            sb.AppendLine($"Balance Remaining: {BalanceRemaining:C}");
            sb.AppendLine($"Is Paid: {(IsPaid == true ? "Yes" : "No")}");
            sb.AppendLine("Items:");

            foreach (var item in LineItems)
            {
                sb.AppendLine($"  - {item.ItemName} | Quantity: {item.Quantity} | Price: {item.ItemPrice:C} | Total: {item.Amount:C}");
            }

            return sb.ToString();
        }

    }

    public class InvoiceLineItemDto
    {
        public string? ItemName { get; set; }
        public int? Quantity { get; set; }
        public double? ItemPrice { get; set; }
        public decimal? Amount { get; set; }

        public override string ToString()
        {
            return $"{ItemName} | Quantity: {Quantity} | Price: {ItemPrice:C} | Total: {Amount:C}";
        }

    }


}
