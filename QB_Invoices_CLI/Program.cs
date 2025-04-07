using QB_Invoices_Lib;

namespace invoice
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Select an option:");
            Console.WriteLine("1: Add an Invoice");
            Console.WriteLine("2: Add Multiple Invoices");
            Console.WriteLine("3: Add Multiple Manually Invoices");
            Console.WriteLine("4: Query All Invoices");

            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    Invoice singleInvoice = PromptInvoiceInput();
                    InvoiceAdder.AddInvoices(new List<Invoice> { singleInvoice });
                    break;

                case "2":
                    List<Invoice> invoiceList = new();
                    while (true)
                    {
                        Invoice inv = PromptInvoiceInput();
                        inv.Memo = "PNW";
                        invoiceList.Add(inv);

                        Console.WriteLine("Add another invoice? (y/n): ");
                        if (Console.ReadLine().Trim().ToLower() != "y") break;
                    }
                    InvoiceAdder.AddInvoices(invoiceList);
                    break;

                case "3":
                    List<Invoice> manualInvoiceList = new List<Invoice>();
                    var invoice1 = new Invoice
                    {
                        CustomerName = "Musharaf Ahmed",
                        TxnDate = DateTime.Now,
                        RefNumber = "INV-123456",
                        Memo = "PNW",
                        LineItems = new List<InvoiceLineItemDto>
                {
                    new InvoiceLineItemDto { ItemName = "Laptop", Quantity = 1 },
                }
                    };
                    var invoice2 = new Invoice
                    {
                        CustomerName = "Shazeb Khan",
                        TxnDate = DateTime.Now,
                        RefNumber = "INV-123457",
                        Memo = "PNW",
                        LineItems = new List<InvoiceLineItemDto>
                {
                    new InvoiceLineItemDto { ItemName = "Laptop", Quantity = 1 },
                    new InvoiceLineItemDto { ItemName = "Mouse", Quantity = 2 },
                    new InvoiceLineItemDto { ItemName = "Keyboard", Quantity = 1 }
                }
                    };
                    var invoice3 = new Invoice
                    {
                        CustomerName = "Mustafa Hussain",
                        TxnDate = DateTime.Now,
                        RefNumber = "INV-123459",
                        Memo = "PNW",
                        LineItems = new List<InvoiceLineItemDto>
                        {

                        new InvoiceLineItemDto { ItemName = "Keyboard", Quantity = 3 }
                        }
                    };

                    manualInvoiceList.Add(invoice1);
                    manualInvoiceList.Add(invoice2);
                    manualInvoiceList.Add(invoice3);
                    InvoiceAdder.AddInvoices(manualInvoiceList);
                    break;

                case "4":
                    // Query and display all invoices
                    try
                    {
                        var allInvoices = Customer_Reader.QueryAllInvoices();
                        if (allInvoices.Count > 0)
                        {
                            foreach (var invoice in allInvoices)
                            {
                                Console.WriteLine(invoice);
                            }
                        }
                        else
                        {
                            Console.WriteLine("No invoices found.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                    break;

                default:
                    Console.WriteLine("Invalid option, please select 1 or 2 or 3 or 4.");
                    break;
            }
        }

        static Invoice PromptInvoiceInput()
        {
            var invoice = new Invoice();

            while (true)
            {
                Console.Write("Enter the Customer Name: ");
                string customerName = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(customerName))
                {
                    Console.WriteLine("Customer Name cannot be empty.");
                    continue;
                }

                invoice.CustomerName = customerName;
                break;
            }

            // Invoice Date (validated)
            while (true)
            {
                Console.Write("Enter the Invoice Date (yyyy-mm-dd): ");
                if (DateTime.TryParse(Console.ReadLine(), out DateTime invoiceDate))
                {
                    invoice.TxnDate = invoiceDate;
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid date format. Please try again.");
                }
            }

            // Invoice Number (mandatory, validated)
            while (true)
            {
                Console.Write("Enter the Invoice Number: ");
                string invoiceNumber = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(invoiceNumber))
                {
                    Console.WriteLine("Invoice Number cannot be empty.");
                    continue;
                }

                invoice.RefNumber = invoiceNumber;
                break;
            }

            // Line Items (at least one required)
            List<InvoiceLineItemDto> lineItems = new();

            while (true)
            {
                Console.Write("Enter Item Name (or type 'done' to finish): ");
                string partName = Console.ReadLine()?.Trim();

                if (partName?.ToLower() == "done")
                {
                    if (lineItems.Count == 0)
                    {
                        Console.WriteLine("At least one item is required.");
                        continue;
                    }
                    break;
                }

                if (string.IsNullOrEmpty(partName))
                {
                    Console.WriteLine("Item Name cannot be empty.");
                    continue;
                }

                int quantity;
                while (true)
                {
                    Console.Write($"Enter quantity for '{partName}': ");
                    if (int.TryParse(Console.ReadLine(), out quantity) && quantity > 0)
                        break;

                    Console.WriteLine("Quantity must be a positive integer.");
                }

                lineItems.Add(new InvoiceLineItemDto
                {
                    ItemName = partName,
                    Quantity = quantity
                });
            }

            invoice.LineItems = lineItems;

            return invoice;
        }

    }

}
