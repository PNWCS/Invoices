using QB_Invoices_Lib;
using System;

namespace invoice
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Select an option:");
            Console.WriteLine("1: Add an Invoice");
            Console.WriteLine("2: Query All Invoices");

            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    // Call the method to create invoice and display the result
                    try
                    {
                        string txnID = new InvoiceAdder().CreateInvoice();
                        if (!string.IsNullOrEmpty(txnID))
                        {
                            Console.WriteLine($"Invoice Created Successfully! Transaction ID: {txnID}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                    break;

                case "2":
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
                    Console.WriteLine("Invalid option, please select 1 or 2.");
                    break;
            }
        }
    }
}
