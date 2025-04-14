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
                case "2":
                case "3":
                case "4":
                    // Query and display all invoices
                    try
                    {
                        var allInvoices = InoviceReader.QueryAllInvoices();
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

    }

}
