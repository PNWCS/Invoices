using System.Diagnostics;
using Serilog;
using QBFC16Lib;
using static QB_Invoices_Test.CommonMethods;

namespace QB_Invoices_Test
{
    [Collection("Sequential Tests")]
    public class EndToEndInvoiceTests
    {
        // 1) Adjust this constant to create a different number of invoices.
        //    We will create the same number of customers, and 2x items.
        private const int INVOICE_COUNT = 2;

        [Fact]
        public void CreateAndDelete_Customers_Items_Invoices()
        {
            // We'll track:
            //   (a) the created QuickBooks IDs (so we can delete them)
            //   (b) additional test data (customer name, item name, price, etc.) for final assertions
            var createdCustomerListIDs = new List<string>();
            var createdItemListIDs = new List<string>();
            var createdInvoiceTxnIDs = new List<string>();

            // We'll also store the random names and prices we used:
            var randomCustomerNames = new List<string>();
            var randomItemNames = new List<string>();
            var randomItemPrices = new List<double>();

            // Each invoice's "test info" so we can assert after reading from QB
            var invoiceTestData = new List<InvoiceTestInfo>();

            try
            {
                // 1) Clean up logs, start fresh
                EnsureLogFileClosed();
                DeleteOldLogFiles();
                ResetLogger();

                // 2) Create N random customers
                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    for (int i = 0; i < INVOICE_COUNT; i++)
                    {
                        // Add customer
                        string randomName = "RandCust_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                        string listID = AddRandomCustomer(qbSession, randomName);
                        createdCustomerListIDs.Add(listID);
                        randomCustomerNames.Add(randomName);
                    }
                }

                // 3) Create 2*N random inventory items
                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    for (int i = 0; i < 2 * INVOICE_COUNT; i++)
                    {
                        // Add item
                        string randomItemName = "RandItem_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                        // Make each item’s price slightly different, e.g. 10.99 + i
                        double randomPrice = 10.99 + i;

                        string listID = AddRandomInventoryItem(qbSession, randomItemName, randomPrice);
                        createdItemListIDs.Add(listID);
                        randomItemNames.Add(randomItemName);
                        randomItemPrices.Add(randomPrice);
                    }
                }

                // 4) Create N invoices
                //    The nth invoice uses the nth customer and the (2*n)th item
                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    for (int i = 0; i < INVOICE_COUNT; i++)
                    {
                        string custListID = createdCustomerListIDs[i];
                        string custName = randomCustomerNames[i];

                        // item index = 2*i
                        int itemIndex = 2 * i;
                        string itemListID = createdItemListIDs[itemIndex];
                        string itemName = randomItemNames[itemIndex];
                        double itemPrice = randomItemPrices[itemIndex];

                        // Consecutive company ID, starting at 100
                        int companyID = 100 + i;

                        // Add the invoice
                        string invoiceTxnID = AddInvoiceReferencing(
                            qbSession,
                            custListID,
                            custName,
                            itemListID,
                            itemName,
                            itemPrice,
                            companyID
                        );
                        createdInvoiceTxnIDs.Add(invoiceTxnID);

                        // Record data for final asserts
                        invoiceTestData.Add(new InvoiceTestInfo
                        {
                            TxnID = invoiceTxnID,
                            CompanyID = companyID,
                            CustomerName = custName,
                            ItemName = itemName,
                            ItemPrice = itemPrice
                        });
                    }
                }

                // 5) Query & verify
                var allInvoices = InvoiceReader.QueryAllInvoices();

                // For each created invoice, assert we see it among the queried results.
                foreach (var invInfo in invoiceTestData)
                {
                    var matchingInvoice = allInvoices.FirstOrDefault(i => i.TxnID == invInfo.TxnID);
                    Assert.NotNull(matchingInvoice);

                    // For example, your InvoiceReader might store the memo as "CompanyID_<num>".
                    // We used "CompanyID_" + companyID in the code below. 
                    string expectedMemo = "CompanyID_" + invInfo.CompanyID;
                    Assert.Equal(expectedMemo, matchingInvoice.CompanyID);

                    // Check the customer name (whatever property your invoice has for that).
                    // e.g., matchingInvoice.CustomerName
                    Assert.Equal(invInfo.CustomerName, matchingInvoice.CustomerName);

                    // Check line items
                    Assert.Single(matchingInvoice.LineItems);

                    var line = matchingInvoice.LineItems.First();
                    Assert.Equal(invInfo.ItemName, line.ItemName);
                    Assert.Equal(2, line.Quantity);   // We hard-coded quantity=2
                    Assert.Equal(invInfo.ItemPrice, line.ItemPrice);
                }
            }
            finally
            {
                // 6) Cleanup EVERYTHING created: invoices first, then items & customers
                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var txnID in createdInvoiceTxnIDs)
                    {
                        DeleteInvoice(qbSession, txnID);
                    }
                }

                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var itemID in createdItemListIDs)
                    {
                        DeleteListObject(qbSession, itemID, ENListDelType.ldtItemInventory);
                    }
                }

                using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var custID in createdCustomerListIDs)
                    {
                        DeleteListObject(qbSession, custID, ENListDelType.ldtCustomer);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a random customer in QB using the given name, returns the QB ListID.
        /// </summary>
        private string AddRandomCustomer(QuickBooksSession qbSession, string customerName)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            ICustomerAdd customerAddRq = request.AppendCustomerAddRq();

            customerAddRq.Name.SetValue(customerName);

            IMsgSetResponse response = qbSession.SendRequest(request);
            return ExtractCustomerListID(response);
        }

        /// <summary>
        /// Creates a random inventory item in QB with the given name and price, returns the QB ListID.
        /// </summary>
        private string AddRandomInventoryItem(QuickBooksSession qbSession, string itemName, double price)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            IItemInventoryAdd itemAddRq = request.AppendItemInventoryAddRq();

            itemAddRq.Name.SetValue(itemName);

            // Minimal required fields for an inventory item:
            itemAddRq.IncomeAccountRef.FullName.SetValue("Sales");
            itemAddRq.AssetAccountRef.FullName.SetValue("Inventory Asset");
            itemAddRq.COGSAccountRef.FullName.SetValue("Cost of Goods Sold");

            // Price
            itemAddRq.SalesPrice.SetValue(price);

            IMsgSetResponse response = qbSession.SendRequest(request);
            return ExtractItemListID(response);
        }

        /// <summary>
        /// Adds an invoice with a single line referencing a specific Customer and Item.
        /// Stores a company ID in the Memo, e.g. 100.
        /// Returns the new Invoice TxnID.
        /// </summary>
        private string AddInvoiceReferencing(
            QuickBooksSession qbSession,
            string customerListID,
            string customerName,
            string itemListID,
            string itemName,
            double itemPrice,
            int companyID
        )
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            IInvoiceAddRq invoiceAddRq = request.AppendInvoiceAddRq();

            // Reference the customer by ListID
            invoiceAddRq.CustomerRef.ListID.SetValue(customerListID);

            // Set an invoice ref number (random or incremental, your choice)
            invoiceAddRq.RefNumber.SetValue("Inv_" + Guid.NewGuid().ToString("N").Substring(0, 5));

            // Use the consecutive company ID in the Memo
            string memo = companyID;
            invoiceAddRq.Memo.SetValue(memo);

            invoiceAddRq.TxnDate.SetValue(DateTime.Today);

            // Add one line
            IORInvoiceLineAdd lineAdd = invoiceAddRq.ORInvoiceLineAddList.Append();
            lineAdd.InvoiceLineAdd.ItemRef.ListID.SetValue(itemListID);
            lineAdd.InvoiceLineAdd.Quantity.SetValue(2);

            // If your QuickBooks setup requires lineAdd.InvoiceLineAdd.ORRatePriceLevel.Rate.SetValue(itemPrice),
            // you'd set that here. Some setups default to the item’s SalesPrice. Adapt as needed.

            IMsgSetResponse response = qbSession.SendRequest(request);
            return ExtractInvoiceTxnID(response);
        }

        /// <summary>
        /// Simple model to store the data we’ll verify in the test.
        /// </summary>
        private class InvoiceTestInfo
        {
            public string TxnID { get; set; } = "";
            public int CompanyID { get; set; }
            public string CustomerName { get; set; } = "";
            public string ItemName { get; set; } = "";
            public double ItemPrice { get; set; }
        }

        // -----------------------------------------------
        // No changes needed below this line, unless you want to adapt:
        // -----------------------------------------------
        private string ExtractCustomerListID(IMsgSetResponse responseMsgSet)
        {
            var responseList = responseMsgSet.ResponseList;
            if (responseList == null || responseList.Count == 0)
                throw new Exception("No response from CustomerAddRq.");

            IResponse response = responseList.GetAt(0);
            if (response.StatusCode != 0)
                throw new Exception($"CustomerAdd failed: {response.StatusMessage}");

            ICustomerRet? custRet = response.Detail as ICustomerRet;
            if (custRet == null)
                throw new Exception("No ICustomerRet returned after adding Customer.");

            return custRet.ListID.GetValue();
        }

        private string ExtractItemListID(IMsgSetResponse responseMsgSet)
        {
            var responseList = responseMsgSet.ResponseList;
            if (responseList == null || responseList.Count == 0)
                throw new Exception("No response from ItemInventoryAddRq.");

            IResponse response = responseList.GetAt(0);
            if (response.StatusCode != 0)
                throw new Exception($"ItemInventoryAdd failed: {response.StatusMessage}");

            IItemInventoryRet? itemRet = response.Detail as IItemInventoryRet;
            if (itemRet == null)
                throw new Exception("No IItemInventoryRet returned after adding Inventory Item.");

            return itemRet.ListID.GetValue();
        }

        private string ExtractInvoiceTxnID(IMsgSetResponse responseMsgSet)
        {
            var responseList = responseMsgSet.ResponseList;
            if (responseList == null || responseList.Count == 0)
                throw new Exception("No response from InvoiceAddRq.");

            IResponse response = responseList.GetAt(0);
            if (response.StatusCode != 0)
                throw new Exception($"InvoiceAdd failed: {response.StatusMessage}");

            IInvoiceRet? invoiceRet = response.Detail as IInvoiceRet;
            if (invoiceRet == null)
                throw new Exception("No IInvoiceRet returned after adding Invoice.");

            return invoiceRet.TxnID.GetValue();
        }

        private void DeleteInvoice(QuickBooksSession qbSession, string txnID)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            ITxnDel txnDelRq = request.AppendTxnDelRq();
            txnDelRq.TxnDelType.SetValue(ENTxnDelType.tdtInvoice);
            txnDelRq.TxnID.SetValue(txnID);

            var responseMsgSet = qbSession.SendRequest(request);
            CheckForError(responseMsgSet, $"Deleting Invoice (TxnID: {txnID})");
        }

        private void DeleteListObject(QuickBooksSession qbSession, string listID, ENListDelType listDelType)
        {
            IMsgSetRequest request = qbSession.CreateRequestSet();
            IListDel listDelRq = request.AppendListDelRq();
            listDelRq.ListDelType.SetValue(listDelType);
            listDelRq.ListID.SetValue(listID);

            var responseMsgSet = qbSession.SendRequest(request);
            CheckForError(responseMsgSet, $"Deleting {listDelType} (ListID: {listID})");
        }

        private void CheckForError(IMsgSetResponse responseMsgSet, string context)
        {
            if (responseMsgSet?.ResponseList == null || responseMsgSet.ResponseList.Count == 0)
                return;

            var response = responseMsgSet.ResponseList.GetAt(0);
            if (response.StatusCode != 0)
            {
                throw new Exception($"Error {context}: {response.StatusMessage}. Status code: {response.StatusCode}");
            }
            else
            {
                Debug.WriteLine($"Successfully completed: {context}");
            }
        }
    }
}
