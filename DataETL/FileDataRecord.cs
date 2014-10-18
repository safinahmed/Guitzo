using System;
using System.Collections.Generic;

namespace DataETLTest
{
    /// <summary>
    /// This class will represent all relevant data in each processed file
    /// TODO: Add ISerializable, IRecord and whatever is necessary
    /// TODO: Add Storing / Retrieving logic 
    /// TODO: Maybe add toXML and/or toJSON logic
    /// </summary>
    public class FileDataRecord : IEnumerable<TransactionRecord>
    {
        private List<TransactionRecord> TransactionRecords;
        public string AccountNumber { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal AccountingBalance { get; set; }


        public TransactionRecord this[int i]
        {
            get
            {
                return TransactionRecords[i];
            }
        }

        public FileDataRecord() : this("",0,0)
        {
        }
        
        public FileDataRecord(string accountNumber, decimal availableBalance, decimal accountingBalance)
        {
            this.TransactionRecords = new List<TransactionRecord>();
            this.AccountNumber = accountNumber;
            this.AvailableBalance = availableBalance;
            this.AccountingBalance = accountingBalance;
        }

        public void AddTransactionRecord(TransactionRecord transactionRecord)
        {
            TransactionRecords.Add(transactionRecord);
        }


        public override string ToString()
        {
            String result = "---- FileDataRecord ----\r\n";
            result += "Account Number: " + AccountNumber + "\r\n";
            result += "Available Balance: " + AvailableBalance + "\r\n";
            result += "Accounting Balance: " + AccountingBalance + "\r\n";

            foreach (TransactionRecord tr in this)
            {
                result += tr.ToString();
            }
            return result;
        }

        IEnumerator<TransactionRecord> IEnumerable<TransactionRecord>.GetEnumerator()
        {
            return TransactionRecords.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return TransactionRecords.GetEnumerator();
        }
    }
}
