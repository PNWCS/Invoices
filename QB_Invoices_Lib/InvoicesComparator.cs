using QB_Invoices_Lib;
using Serilog;

namespace QB_Invoices_Lib
{
    public class InvoicesComparator
    {
        // Static constructor runs ONCE when the class is first used  
        static InvoicesComparator()
        {
            LoggerConfig.ConfigureLogging(); // Safe to call (only initializes once)  
            Log.Information("InvoicesComparator Initialized.");
        }

        public static List<Invoice> CompareInvoices(List<Invoice> companyInvoices)
        {
            // Read QB invoices  
            List<Invoice> qbInvoices = InvoiceReader.QueryAllInvoices();

            // Convert QuickBooks and Company invoices into dictionaries for quick lookup  
            var qbInvoiceDict = qbInvoices
                .Where(i => i.InvoiceNumber != null) // Filter out null InvoiceNumbers  
                .ToDictionary(i => i.InvoiceNumber!, i => i); // Use null-forgiving operator  

            var companyInvoiceDict = companyInvoices
                .Where(i => i.InvoiceNumber != null) // Filter out null InvoiceNumbers  
                .ToDictionary(i => i.InvoiceNumber!, i => i); // Use null-forgiving operator  

            List<Invoice> newInvoicesToAdd = new List<Invoice>();

            // Iterate through company invoices to compare with QB invoices  
            foreach (var companyInvoice in companyInvoices)
            {
                if (companyInvoice.InvoiceNumber == null)
                {
                    continue; // Skip invoices with null InvoiceNumber  
                }
                if (qbInvoiceDict.TryGetValue(companyInvoice.InvoiceNumber, out var qbInvoice))
                {
                    // Invoice exists in both, compare important fields (you can adjust based on what matters)  
                    if (qbInvoice.InoviceAmount == companyInvoice.InoviceAmount && qbInvoice.InvoiceDate == companyInvoice.InvoiceDate && qbInvoice.Memo == companyInvoice.Memo && qbInvoice.CustomerName == companyInvoice.CustomerName)
                    {
                        companyInvoice.Status = InvoiceStatus.Unchanged;
                    }
                    else
                    {
                        companyInvoice.Status = InvoiceStatus.Different;
                    }
                }
                else
                {
                    // Invoice does not exist in QB, queue for addition  
                    companyInvoice.Status = InvoiceStatus.Added;
                    newInvoicesToAdd.Add(companyInvoice);
                }
            }

            // Check for invoices that exist in QB but not in the company file  
            foreach (var qbInvoice in qbInvoices)
            {
                if (qbInvoice.InvoiceNumber == null || !companyInvoiceDict.ContainsKey(qbInvoice.InvoiceNumber))
                {
                    qbInvoice.Status = InvoiceStatus.Missing;
                    companyInvoiceDict[qbInvoice.InvoiceNumber!] = qbInvoice;
                }
            }

            // Call InvoicesAdder to add new invoices to QuickBooks  
            if (newInvoicesToAdd.Count > 0)
            {
                InvoiceAdder.AddInvoices(newInvoicesToAdd);

                // Ensure `Added` invoices are updated in `companyInvoiceDict`  
                foreach (var addedInvoice in newInvoicesToAdd)
                {
                    if (addedInvoice.InvoiceNumber != null && companyInvoiceDict.TryGetValue(addedInvoice.InvoiceNumber, out var companyInvoice))
                    {
                        companyInvoice.Status = addedInvoice.Status;
                    }
                }
            }

            // Merge `companyInvoiceDict` with `qbInvoiceDict` (removing duplicates)  
            Dictionary<string, Invoice> mergedInvoicesDict = new Dictionary<string, Invoice>();

            foreach (var invoice in qbInvoiceDict.Values)
                mergedInvoicesDict[invoice.InvoiceNumber!] = invoice; // Add all QB invoices  

            foreach (var invoice in companyInvoiceDict.Values)
                mergedInvoicesDict[invoice.InvoiceNumber!] = invoice; // Overwrite with company invoices  

            // Convert merged dictionary back to a list  
            Log.Information("InvoicesComparator Completed");
            foreach (var invoice in mergedInvoicesDict.Values)
            {
                Log.Information($"Invoice {invoice.InvoiceNumber} is {invoice.Status}.");
            }
            return mergedInvoicesDict.Values.ToList();
        }
    }
}
