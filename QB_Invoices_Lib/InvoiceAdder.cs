using QB_Invoices_Lib;
using QBFC16Lib;

public class InvoiceAdder
{
    public static void AddInvoices(List<Invoice> invoices)
    {
        using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
        {
            foreach (var invoice in invoices)
            {
                Console.WriteLine($" Processing invoice for customer: {invoice.CustomerName}");

                if (!invoice.InvoiceDate.HasValue)
                {
                    Console.WriteLine(" Error: Invoice date is missing.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
                {
                    Console.WriteLine(" Error: Invoice number is missing.");
                    continue;
                }

                string customerListID = GetCustomerListID(qbSession, invoice.CustomerName);
                if (string.IsNullOrEmpty(customerListID))
                {
                    Console.WriteLine($" Error: Customer '{invoice.CustomerName}' not found in QuickBooks.");
                    continue;
                }

                var itemList = new List<(string itemListID, int quantity)>();

                foreach (var item in invoice.LineItems)
                {
                    if (string.IsNullOrWhiteSpace(item.ItemName))
                    {
                        Console.WriteLine(" Skipping item with empty name.");
                        continue;
                    }

                    string itemListID = GetItemListID(qbSession, item.ItemName);
                    if (string.IsNullOrEmpty(itemListID))
                    {
                        Console.WriteLine($" Error: Item '{item.ItemName}' not found in QuickBooks.");
                        continue;
                    }

                    if (!item.Quantity.HasValue || item.Quantity <= 0)
                    {
                        Console.WriteLine($" Error: Invalid quantity for item '{item.ItemName}'.");
                        continue;
                    }

                    itemList.Add((itemListID, item.Quantity.Value));
                }

                if (itemList.Count == 0)
                {
                    Console.WriteLine(" Skipping invoice: no valid line items.");
                    continue;
                }

                try
                {
                    string txnID = CreateInvoice(qbSession, customerListID, invoice.InvoiceDate.Value, invoice.InvoiceNumber, invoice.Memo, itemList);
                    invoice.TxnID = txnID;
                    Console.WriteLine($" Invoice created successfully. TxnID: {txnID}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" Failed to create invoice: {ex.Message}");
                }
            }
        }
    }


    private static string GetCustomerListID(QuickBooksSession qbSession, string customerName)
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

    private static string GetItemListID(QuickBooksSession qbSession, string itemName)
    {
        // Log the item being searched
        //Console.WriteLine($"Searching for item: {itemName}");

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
                //Console.WriteLine($"Item found: {itemNameFound}, ListID: {itemListID}");
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


    private static string CreateInvoice(QuickBooksSession qbSession, string customerListID, DateTime invoiceDate, string invoiceNumber, string memo, List<(string itemListID, int quantity)> items)
    {
        IMsgSetRequest request = qbSession.CreateRequestSet();
        IInvoiceAdd invoiceAddRq = request.AppendInvoiceAddRq();

        invoiceAddRq.CustomerRef.ListID.SetValue(customerListID);
        invoiceAddRq.TxnDate.SetValue(invoiceDate);
        invoiceAddRq.RefNumber.SetValue(invoiceNumber);
        invoiceAddRq.Memo.SetValue(memo);

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
