using System.Text;

namespace QB_Invoices_Lib
{
    public class Invoice
    {

        public string? TxnID { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? Memo { get; set; }
        public decimal? InoviceAmount { get; set; }
        public decimal? BalanceRemaining { get; set; }
        public string? CompanyID { get; set; }

        public List<InvoiceLineItemDto> LineItems { get; set; } = new List<InvoiceLineItemDto>(); // New property for parts

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Invoice TxnID: {TxnID}");
            sb.AppendLine($"Customer Name: {CustomerName}");
            sb.AppendLine($"Invoice Date: {InvoiceDate?.ToShortDateString()}");
            sb.AppendLine($"Invoice Number: {InvoiceNumber}");
            sb.AppendLine($"Total Amount: {InoviceAmount:C}");
            sb.AppendLine($"Balance Remaining: {BalanceRemaining:C}");
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
