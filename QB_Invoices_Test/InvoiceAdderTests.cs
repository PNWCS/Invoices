using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using QBFC16Lib;                       // QuickBooks Desktop SDK
using Serilog;
using QB_Invoices_Lib;                 // Invoice + DTO models
using static QB_Invoices_Test.CommonMethods;

namespace QB_Invoices_Test
{
    [Collection("Sequential Tests")]    // QBFC must be single‑threaded
    public class InvoiceAdderTests
    {
        private const int INVOICE_COUNT = 3;   // adjust as needed

        //------------------------------------------------------------------
        //  MAIN TEST
        //------------------------------------------------------------------
        [Fact]
        public void AddInvoices_EndToEnd_PersistedCorrectly()
        {
            var createdCustomerIds = new List<string>();
            var createdItemIds = new List<string>();
            var addedInvoiceTxnIds = new List<string>();

            var customerNames = new List<string>();
            var itemNames = new List<string>();
            var itemPrices = new List<double>();
            var companyIds = new List<int>();

            try
            {
                // 0) reset logs
                EnsureLogFileClosed();
                DeleteOldLogFiles();
                ResetLogger();

                //----------------------------------------------------------
                // 1)  Create random customers
                //----------------------------------------------------------
                using (var qb = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    for (int i = 0; i < INVOICE_COUNT; i++)
                    {
                        string name = "Cust_" + Guid.NewGuid().ToString("N")[..6];
                        string id = AddCustomer(qb, name);

                        createdCustomerIds.Add(id);
                        customerNames.Add(name);
                    }
                }

                //----------------------------------------------------------
                // 2)  Create random inventory items
                //----------------------------------------------------------
                using (var qb = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    for (int i = 0; i < INVOICE_COUNT; i++)
                    {
                        string name = "Item_" + Guid.NewGuid().ToString("N")[..6];
                        double price = 12.99 + i;

                        string id = AddInventoryItem(qb, name, price);

                        createdItemIds.Add(id);
                        itemNames.Add(name);
                        itemPrices.Add(price);
                    }
                }

                //----------------------------------------------------------
                // 3)  Build Invoice DTOs
                //----------------------------------------------------------
                var invoices = new List<Invoice>();
                for (int i = 0; i < INVOICE_COUNT; i++)
                {
                    int companyId = 900 + i;   // arbitrary unique
                    companyIds.Add(companyId);

                    invoices.Add(new Invoice
                    {
                        CustomerName = customerNames[i],
                        InvoiceDate = DateTime.Today,
                        InvoiceNumber = "INV_" + Guid.NewGuid().ToString("N")[..5],
                        Memo = $"CompanyID_{companyId}",
                        CompanyID = companyId.ToString(),
                        LineItems = new List<InvoiceLineItemDto>
                        {
                            new InvoiceLineItemDto
                            {
                                ItemName  = itemNames[i],
                                Quantity  = 2,
                                ItemPrice = itemPrices[i],
                                Amount    = (decimal)(itemPrices[i] * 2)
                            }
                        }
                    });
                }

                //----------------------------------------------------------
                // 4)  Call code under test
                //----------------------------------------------------------
                InvoiceAdder.AddInvoices(invoices);

                //----------------------------------------------------------
                // 5)  Ensure every invoice has TxnID
                //----------------------------------------------------------
                foreach (var inv in invoices)
                    Assert.False(string.IsNullOrWhiteSpace(inv.TxnID),
                                 $"Missing TxnID for invoice {inv.InvoiceNumber}");

                //----------------------------------------------------------
                // 6)  Query QB directly to verify
                //----------------------------------------------------------
                using (var qb = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var inv in invoices)
                    {
                        var qbInv = QueryInvoice(qb, inv.TxnID!);
                        Assert.NotNull(qbInv);

                        Assert.Equal(inv.CustomerName, qbInv.CustomerRef.FullName.GetValue());
                        Assert.Equal(inv.Memo, qbInv.Memo?.GetValue());

                        // one line item check
                        // Assert.Single(qbInv.ORInvoiceLineRetList);
                        var line = qbInv.ORInvoiceLineRetList.GetAt(0).InvoiceLineRet;

                        Assert.Equal(inv.LineItems[0].ItemName, line.ItemRef.FullName.GetValue());
                        Assert.Equal(inv.LineItems[0].Quantity, line.Quantity?.GetValue());
                    }
                }

                addedInvoiceTxnIds = invoices.Select(i => i.TxnID!).ToList();
            }
            finally
            {
                //----------------------------------------------------------
                // 7)  Clean up (invoices → items → customers)
                //----------------------------------------------------------
                using (var qb = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var t in addedInvoiceTxnIds)
                        DeleteInvoice(qb, t);
                }
                using (var qb = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var id in createdItemIds)
                        DeleteListObj(qb, id, ENListDelType.ldtItemInventory);
                }
                using (var qb = new QuickBooksSession(AppConfig.QB_APP_NAME))
                {
                    foreach (var id in createdCustomerIds)
                        DeleteListObj(qb, id, ENListDelType.ldtCustomer);
                }
            }
        }

        //------------------------------------------------------------------
        //  Local helper methods
        //------------------------------------------------------------------
        private string AddCustomer(QuickBooksSession qb, string name)
        {
            var rq = qb.CreateRequestSet();
            rq.AppendCustomerAddRq().Name.SetValue(name);
            var rs = qb.SendRequest(rq);
            return ExtractCustomerListID(rs);
        }

        private string AddInventoryItem(QuickBooksSession qb, string name, double price)
        {
            var rq = qb.CreateRequestSet();
            var add = rq.AppendItemInventoryAddRq();
            add.Name.SetValue(name);
            add.IncomeAccountRef.FullName.SetValue("Sales");
            add.AssetAccountRef.FullName.SetValue("Inventory Asset");
            add.COGSAccountRef.FullName.SetValue("Cost of Goods Sold");
            add.SalesPrice.SetValue(price);

            var rs = qb.SendRequest(rq);
            return ExtractItemListID(rs);
        }

        private IInvoiceRet QueryInvoice(QuickBooksSession qb, string txnID)
        {
            var rq = qb.CreateRequestSet();
            var q = rq.AppendInvoiceQueryRq();
            q.IncludeLineItems.SetValue(true);
            q.ORInvoiceQuery.TxnIDList.Add(txnID);

            var rs = qb.SendRequest(rq);
            CheckForError(rs, $"InvoiceQuery {txnID}");

            var list = rs.ResponseList.Count > 0
                       ? rs.ResponseList.GetAt(0).Detail as IInvoiceRetList
                       : null;
            return list?.Count > 0 ? list.GetAt(0) : null;
        }

        private void DeleteInvoice(QuickBooksSession qb, string txnID)
        {
            var rq = qb.CreateRequestSet();
            var del = rq.AppendTxnDelRq();
            del.TxnDelType.SetValue(ENTxnDelType.tdtInvoice);
            del.TxnID.SetValue(txnID);

            CheckForError(qb.SendRequest(rq), $"Delete Invoice {txnID}");
        }

        private void DeleteListObj(QuickBooksSession qb, string listID, ENListDelType delType)
        {
            var rq = qb.CreateRequestSet();
            var del = rq.AppendListDelRq();
            del.ListDelType.SetValue(delType);
            del.ListID.SetValue(listID);

            CheckForError(qb.SendRequest(rq), $"Delete {delType} {listID}");
        }

        //------------------------------------------------------------------
        //  SDK‑response helpers (kept private to this test file)
        //------------------------------------------------------------------
        private static void CheckForError(IMsgSetResponse rs, string context)
        {
            if (rs?.ResponseList == null || rs.ResponseList.Count == 0)
                return;

            var resp = rs.ResponseList.GetAt(0);
            if (resp.StatusCode != 0)
                throw new Exception($"Error during {context}: {resp.StatusMessage} (Code={resp.StatusCode})");
            else
                Debug.WriteLine($"[{DateTime.Now:T}] {context} OK");
        }

        private static string ExtractCustomerListID(IMsgSetResponse rs)
        {
            var resp = rs.ResponseList.GetAt(0);
            if (resp.StatusCode != 0) throw new Exception(resp.StatusMessage);
            return ((ICustomerRet)resp.Detail!).ListID.GetValue();
        }

        private static string ExtractItemListID(IMsgSetResponse rs)
        {
            var resp = rs.ResponseList.GetAt(0);
            if (resp.StatusCode != 0) throw new Exception(resp.StatusMessage);
            return ((IItemInventoryRet)resp.Detail!).ListID.GetValue();
        }
    }
}