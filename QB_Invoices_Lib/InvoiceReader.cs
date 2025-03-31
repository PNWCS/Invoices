using QBFC16Lib;
using System;

namespace QB_Invoices_Lib
{
    public class Customer_Reader
    {
        public static List<Invoice> QueryAllInvoices()
        {
            using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                IMsgSetRequest requestMsgSet = qbSession.CreateRequestSet();
                IInvoiceQuery invoiceQueryRq = requestMsgSet.AppendInvoiceQueryRq();
                invoiceQueryRq.IncludeLineItems.SetValue(true);
                IMsgSetResponse responseMsgSet = qbSession.SendRequest(requestMsgSet);
                List<Invoice> invoices = new List<Invoice>();
                IResponseList responseList = responseMsgSet.ResponseList;
                if (responseList == null || responseList.Count == 0)
                    throw new Exception("No response from InvoiceQueryRq.");

                IResponse response = responseList.GetAt(0);
                if (response.StatusCode != 0)
                    throw new Exception($"InvoiceQuery failed: {response.StatusMessage}");

                IInvoiceRetList invoiceList = response.Detail as IInvoiceRetList;
                if (invoiceList == null)
                    throw new Exception("No IInvoiceRetList returned.");

                for (int i = 0; i < invoiceList.Count; i++)
                {
                    IInvoiceRet invoice = invoiceList.GetAt(i);
                    invoices.Add(InvoiceMapper.MapToDto(invoice));
                }

                return invoices;
            }
        }
    }
}