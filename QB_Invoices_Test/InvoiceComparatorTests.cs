// QB_Invoices_Test/InvoicesComparatorTests.cs
using System.Diagnostics;
using Serilog;
using QB_Invoices_Lib;
using QB_Invoices_Lib.Compare;           // assumes InvoicesComparator lives here
using QBFC16Lib;
using static QB_Invoices_Test.CommonMethods;

namespace QB_Invoices_Test
{
    [Collection("Sequential Tests")]      // prevent concurrent QB access
    public class InvoicesComparatorTests
    {
        private const string TEST_CUSTOMER = "SyncTestCustomer";

        [Fact]
        public void CompareInvoices_FullLifecycleScenario_VerifyStatusesAndLogs()
        {
            // 0️⃣  House-keeping ─ log setup
            EnsureLogFileClosed();
            DeleteOldLogFiles();
            ResetLogger();

            // 1️⃣  Create five brand-new in-memory invoices (none exist in QB yet)
            var initialInvoices = BuildRandomInvoices(5);

            // 2️⃣  FIRST compare – every invoice should be Added ➜ QB
            var firstCompare = InvoicesComparator.CompareInvoices(initialInvoices);

            foreach (var inv in firstCompare.Where(i => initialInvoices.Any(x => x.CompanyID == i.CompanyID)))
                Assert.Equal(InvoiceStatus.Added, inv.Status);

            // 3️⃣  Mutate data set → Missing, Different, Unchanged scenarios
            var updatedInvoices = new List<Invoice>(initialInvoices);

            var missingInv  = updatedInvoices[0];                 // simulate “Missing”
            var diffInv     = updatedInvoices[1];                 // simulate “Different”
            updatedInvoices.Remove(missingInv);
            diffInv.Memo += " – edited";                          // trivial change

            // 4️⃣  SECOND compare – verify status transitions
            var secondCompare = InvoicesComparator.CompareInvoices(updatedInvoices)
                                                   .ToDictionary(i => i.CompanyID);

            Assert.Equal(InvoiceStatus.Missing,   secondCompare[missingInv.CompanyID].Status);
            Assert.Equal(InvoiceStatus.Different, secondCompare[diffInv.     CompanyID].Status);

            foreach (var inv in updatedInvoices.Where(i => i.CompanyID != diffInv.CompanyID))
                Assert.Equal(InvoiceStatus.Unchanged, secondCompare[inv.CompanyID].Status);

            // 5️⃣  Clean-up – delete test invoices from QB
            try
            {
                var added = firstCompare.Where(i => !string.IsNullOrEmpty(i.TxnID)).ToList();
                if (added.Count > 0)
                {
                    using var qb = new QuickBooksSession(AppConfig.QB_APP_NAME);
                    foreach (var inv in added) DeleteInvoice(qb, inv.TxnID!);
                }
            }
            finally
            {
                EnsureLogFileClosed();
            }

            // 6️⃣  Verify Serilog output
            var logFile = GetLatestLogFile();
            EnsureLogFileExists(logFile);
            var logs = File.ReadAllText(logFile);

            Assert.Contains("InvoicesComparator Initialized", logs);
            Assert.Contains("InvoicesComparator Completed",   logs);

            void AssertStatusLogged(IEnumerable<Invoice> list)
            {
                foreach (var inv in list)
                    Assert.Contains($"Invoice {inv.InvoiceNumber} is {inv.Status}.", logs);
            }
            AssertStatusLogged(firstCompare);
            AssertStatusLogged(secondCompare.Values);
        }

        // ----------  helpers  ----------
        private static List<Invoice> BuildRandomInvoices(int count)
        {
            var list = new List<Invoice>();
            var rand = new Random();

            for (int i = 0; i < count; i++)
            {
                string companyId = Guid.NewGuid().ToString("N").Substring(0, 8);
                var inv = new Invoice
                {
                    CompanyID      = companyId,
                    CustomerName   = TEST_CUSTOMER,
                    InvoiceDate    = DateTime.Today,
                    InvoiceNumber  = $"SYNC-{rand.Next(10000, 99999)}",
                    Memo           = "Auto-generated test invoice",
                    InoviceAmount  = 100 + i,
                    BalanceRemaining = 100 + i,
                    LineItems = new List<InvoiceLineItemDto>
                    {
                        new InvoiceLineItemDto
                        {
                            ItemName = "TestItem",
                            Quantity = 1,
                            ItemPrice = 100 + i,
                            Amount = 100 + i
                        }
                    }
                };
                list.Add(inv);
            }
            return list;
        }

        private static void DeleteInvoice(QuickBooksSession qb, string txnID)
        {
            IMsgSetRequest rq = qb.CreateRequestSet();
            ITxnDel del = rq.AppendTxnDelRq();
            del.TxnDelType.SetValue(ENTxnDelType.tdtInvoice);
            del.TxnID.SetValue(txnID);

            IMsgSetResponse rs = qb.SendRequest(rq);
            IResponse resp = rs.ResponseList?.GetAt(0);
            Debug.WriteLine(resp?.StatusCode == 0
                ? $"✔ Deleted test invoice {txnID}"
                : $"✖ Failed to delete invoice {txnID}: {resp?.StatusMessage}");
        }
    }
}
