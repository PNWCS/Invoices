using System;
using System.Collections.Generic;
using System.Linq;
using QB_Invoices_Lib;
using QBFC16Lib;
using static System.Collections.Specialized.BitVector32;

public class InvoiceAdder
{
    public string CreateInvoice()
    {
        using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
        {
            // Taking user input for customer name, invoice date, invoice number, invoice amount
            Console.WriteLine("Enter the Customer Name: ");
            string customerName = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(customerName))
            {
                Console.WriteLine("Error: Customer Name cannot be empty.");
                return string.Empty;
            }

            // Check if the customer exists in QuickBooks
            string customerListID = GetCustomerListID(qbSession, customerName);
            if (string.IsNullOrEmpty(customerListID))
            {
                Console.WriteLine("Error: Customer not found in QuickBooks.");
                return string.Empty;
            }

            DateTime invoiceDate;
            Console.WriteLine("Enter the Invoice Date (yyyy-mm-dd): ");
            if (!DateTime.TryParse(Console.ReadLine(), out invoiceDate))
            {
                Console.WriteLine("Error: Invalid Date format.");
                return string.Empty;
            }



            Console.WriteLine("Enter the Invoice Number: ");
            string invoiceNumber = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(invoiceNumber))
            {
                Console.WriteLine("Error: Invoice Number cannot be empty.");
                return string.Empty;
            }

            // Taking user input for parts and quantities
            List<(string itemListID, int quantity)> items = new List<(string itemListID, int quantity)>();
            while (true)
            {
                Console.WriteLine("Enter Item Name (or type 'done' to finish): ");
                string partName = Console.ReadLine()?.Trim();
                if (partName?.ToLower() == "done")
                    break;

                if (string.IsNullOrEmpty(partName))
                {
                    Console.WriteLine("Error: Item Name cannot be empty.");
                    continue;
                }

                string itemListID = GetItemListID(qbSession, partName);
                if (string.IsNullOrEmpty(itemListID))
                {
                    Console.WriteLine($"Error: Item '{partName}' not found in QuickBooks.");
                    continue;
                }

                Console.WriteLine($"Enter quantity for {partName}: ");
                int quantity;
                if (!int.TryParse(Console.ReadLine(), out quantity) || quantity <= 0)
                {
                    Console.WriteLine("Error: Quantity must be a positive integer.");
                    continue;
                }

                items.Add((itemListID, quantity));
            }

            if (items.Count == 0)
            {
                Console.WriteLine("Error: No parts were added to the invoice.");
                return string.Empty;
            }

            // Call the method to create the invoice
            try
            {
                return CreateInvoice(qbSession, customerListID, invoiceDate, invoiceNumber, items);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Failed to create invoice. {ex.Message}");
                return string.Empty;
            }
        }
    }

    private string GetCustomerListID(QuickBooksSession qbSession, string customerName)
    {
        IMsgSetRequest request = qbSession.CreateRequestSet();
        ICustomerQuery customerQueryRq = request.AppendCustomerQueryRq();
        customerQueryRq.ORCustomerListQuery.CustomerListFilter.ORNameFilter.NameFilter.MatchCriterion.SetValue(ENMatchCriterion.mcContains);
        customerQueryRq.ORCustomerListQuery.CustomerListFilter.ORNameFilter.NameFilter.Name.SetValue(customerName);

        IMsgSetResponse response = qbSession.SendRequest(request);
        IResponse qbResponse = response.ResponseList.GetAt(0);

        if (qbResponse.StatusCode == 0 && qbResponse.Detail is ICustomerRetList customerList && customerList.Count > 0)
        {
            return customerList.GetAt(0).ListID.GetValue();
        }
        return string.Empty;
    }

    private string GetItemListID(QuickBooksSession qbSession, string itemName)
    {
        // Log the item being searched
        Console.WriteLine($"Searching for item: {itemName}");

        IMsgSetRequest request = qbSession.CreateRequestSet();
        IItemQuery itemQueryRq = request.AppendItemQueryRq();

        // Apply filter to match the item name
        itemQueryRq.ORListQuery.ListFilter.ORNameFilter.NameFilter.MatchCriterion.SetValue(ENMatchCriterion.mcContains);
        itemQueryRq.ORListQuery.ListFilter.ORNameFilter.NameFilter.Name.SetValue(itemName);

        // Send the request to QuickBooks
        IMsgSetResponse response = qbSession.SendRequest(request);
        IResponse qbResponse = response.ResponseList.GetAt(0);

        if (qbResponse.StatusCode == 0 && qbResponse.Detail is IORItemRetList itemList && itemList.Count > 0)
        {
            // Item found, log and return ListID
            IORItemRet itemRet = itemList.GetAt(0);
            string itemNameFound = itemRet.ItemServiceRet?.Name?.GetValue() ??
                                   itemRet.ItemNonInventoryRet?.Name?.GetValue() ??
                                   itemRet.ItemInventoryRet?.Name?.GetValue() ??
                                   itemRet.ItemOtherChargeRet?.Name?.GetValue() ??
                                   itemRet.ItemInventoryAssemblyRet?.Name?.GetValue() ??
                                   itemRet.ItemFixedAssetRet?.Name?.GetValue() ??
                                   itemRet.ItemSubtotalRet?.Name?.GetValue() ??
                                   itemRet.ItemDiscountRet?.Name?.GetValue() ??
                                   itemRet.ItemPaymentRet?.Name?.GetValue() ??
                                   itemRet.ItemSalesTaxRet?.Name?.GetValue() ??
                                   itemRet.ItemSalesTaxGroupRet?.Name?.GetValue() ??
                                   itemRet.ItemGroupRet?.Name?.GetValue();

            string itemListID = itemRet.ItemServiceRet?.ListID?.GetValue() ??
                                itemRet.ItemNonInventoryRet?.ListID?.GetValue() ??
                                itemRet.ItemInventoryRet?.ListID?.GetValue() ??
                                itemRet.ItemOtherChargeRet?.ListID?.GetValue() ??
                                itemRet.ItemInventoryAssemblyRet?.ListID?.GetValue() ??
                                itemRet.ItemFixedAssetRet?.ListID?.GetValue() ??
                                itemRet.ItemSubtotalRet?.ListID?.GetValue() ??
                                itemRet.ItemDiscountRet?.ListID?.GetValue() ??
                                itemRet.ItemPaymentRet?.ListID?.GetValue() ??
                                itemRet.ItemSalesTaxRet?.ListID?.GetValue() ??
                                itemRet.ItemSalesTaxGroupRet?.ListID?.GetValue() ??
                                itemRet.ItemGroupRet?.ListID?.GetValue();

            if (!string.IsNullOrEmpty(itemNameFound) && !string.IsNullOrEmpty(itemListID))
            {
                Console.WriteLine($"Item found: {itemNameFound}, ListID: {itemListID}");
                return itemListID;
            }
            else
            {
                Console.WriteLine("Item is of an unsupported type.");
                return string.Empty;
            }
        }
        else
        {
            // No item found
            Console.WriteLine($"Item '{itemName}' not found or failed to retrieve.");
            return string.Empty;
        }
    }


    private string CreateInvoice(QuickBooksSession qbSession, string customerListID, DateTime invoiceDate, string invoiceNumber, List<(string itemListID, int quantity)> items)
    {
        IMsgSetRequest request = qbSession.CreateRequestSet();
        IInvoiceAdd invoiceAddRq = request.AppendInvoiceAddRq();

        invoiceAddRq.CustomerRef.ListID.SetValue(customerListID);
        invoiceAddRq.TxnDate.SetValue(invoiceDate);
        invoiceAddRq.RefNumber.SetValue(invoiceNumber);
        // invoiceAddRq.Memo.SetValue($"Invoice Amount: {invoiceAmount}");

        foreach (var item in items)
        {
            IORInvoiceLineAdd lineAdd = invoiceAddRq.ORInvoiceLineAddList.Append();
            lineAdd.InvoiceLineAdd.ItemRef.ListID.SetValue(item.itemListID);
            lineAdd.InvoiceLineAdd.Quantity.SetValue(item.quantity);
        }

        IMsgSetResponse response = qbSession.SendRequest(request);
        IResponse qbResponse = response.ResponseList.GetAt(0);

        if (qbResponse.StatusCode != 0)
        {
            throw new Exception($"Invoice creation failed: {qbResponse.StatusMessage}");
        }

        IInvoiceRet invoiceRet = qbResponse.Detail as IInvoiceRet;
        return invoiceRet?.TxnID.GetValue() ?? throw new Exception("Failed to retrieve Invoice TxnID.");
    }
}
