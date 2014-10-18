using System;

namespace DataETLTest
{
    /// <summary>
    /// This class will represent all the data in each transaction record
    /// TODO: Add ISerializable, IRecord and whatever is necessary
    /// TODO: Add Storing /Retrieving logic
    /// TODO: Maybe add toJSON and/or toXML logic
    /// </summary>
    public class TransactionRecord
    {

        public string OriginalDescription { get; set; }

        public string ParsedDescription { get; set; }

        public string ExtraInfo { get; set; }

        public decimal Amount { get; set; }

        public DateTime Date { get; set; }

        public int Type { get; set; }

        public string Category { get; set; }

        public TransactionRecord(string originalDescription, decimal amount, DateTime date, int type, string parsedDescription = null, string extraInfo = null)
        {
            this.OriginalDescription = originalDescription;
            this.ParsedDescription = parsedDescription;
            this.ExtraInfo = extraInfo;
            this.Amount = amount;
            this.Date = date;
            this.Type = type;
            this.Category = "";
        }

        public override string ToString()
        {
            String result = "---- ETLREcord ----\r\n";
            result += "Original Description: " + OriginalDescription + "\r\n";
            result += "Parsed Description: " + ParsedDescription + "\r\n";
           // result += "Extra Info: " + ExtraInfo + "\r\n";
            result += "Date: " + Date + "\r\n";
            result += "Amount: " + Amount + "\r\n";
            result += "Type: " + Type + "\r\n";
            return result;
        }

    }
}
