using System;
using System.Text.RegularExpressions;
using Prediction;

namespace DataETLTest
{
    /// <summary>
    /// Connector to parse data from a BES Export
    /// </summary>
    class MontepioConnector : IETLConnector
    {

        private FileDataRecord dataRecord;

        public MontepioConnector()
        {
            dataRecord =  new FileDataRecord();
        }

        public FileDataRecord Extract(byte[] fileData)
        {
            //Set known indexes
            const int accountNumberIndex = 3;
            const int balanceIndex = 0;

            const int dateIndex = 0;
            const int descriptionIndex = 2;
            const int valueIndex = 3;

            if (fileData == null)
            {
                throw new ETLException("fileData cannot be null");
            }

            try
            {
                var csv = new CSVUtil(fileData, "\t");

                //Skip Header
                csv.Next();
                csv.Next();

                string accountNumber = csv[accountNumberIndex];
                dataRecord.AccountNumber = ParseAccountNumber(accountNumber);

                //Skip Headers and Irrelevant Data
                csv.SkipLines(3);
                csv.Next();

                dataRecord.AccountingBalance = Convert.ToDecimal(csv[balanceIndex], Constants.CulturePT);

                //Skip Headers and Irrelevant Data
                csv.Next();
                csv.Next();

                dataRecord.AvailableBalance = Convert.ToDecimal(csv[balanceIndex], Constants.CulturePT);

                //Skip Header
                csv.Next();

                while (csv.Next())
                {
                    //If there is no date, file is over
                    if (csv[dateIndex] == "")
                        break;

                    DateTime date = Convert.ToDateTime(csv[dateIndex], Constants.CulturePT);
                    string description = csv[descriptionIndex];
                    string value = csv[valueIndex];
                    decimal amount;
                    int type;

                    GetAmountAndType(value, description, out amount, out type);

                    var etlRecord = new TransactionRecord(description, amount, date, type);

                    dataRecord.AddTransactionRecord(etlRecord);
                }
            }
            catch (Exception ex)
            {
                throw new ETLException("Could not extract Montepio Data: " + ex.Message);
            }

            return dataRecord;
        }

        public FileDataRecord Transform()
        {
            try
            {
                foreach (TransactionRecord tr in dataRecord)
                {
                    String extraInfo;
                    tr.ParsedDescription = ParseDescription(tr.OriginalDescription, out extraInfo);
                    tr.ExtraInfo = extraInfo;

                }
            }
            catch (Exception ex)
            {
                throw new ETLException("Could not transform Montepio Data: " + ex.Message);
            }


            return dataRecord;
        }

        public FileDataRecord Categorize()
        {
            try
            {
                foreach (TransactionRecord transactionRecord in dataRecord)
                {
                    transactionRecord.Category = PredictionEngine.Predict(transactionRecord.ParsedDescription,
                                                            PredictionEngine.NAIVE_BAYES);
                }
            }
            catch (Exception ex)
            {
                throw new ETLException("Could not categorize Montepio Data: " + ex.Message);
            }

            return dataRecord;
        }

        public void Load()
        {
            throw new NotImplementedException();
        }

        internal string ParseDescription(string description, out string extraInfo)
        {
            String result = description;
            extraInfo = "";

            //Remove initial COMPRA\*
            Regex rgx = new Regex("^COMPRA/[0-9]*");
            result = rgx.Replace(result, "").Trim();

            //Remove initial COMPRA ELEC 4011676/01
            rgx = new Regex("^TR-");
            result = rgx.Replace(result, "TRANSFERENCIA ").Trim();

            //Remove multiple spaces
            rgx = new Regex("\\s\\s+");
            result = rgx.Replace(result, " ").Trim();

            result = Utils.CleanCompanyName(result);

            result = Constants.TextInfo.ToTitleCase(Constants.TextInfo.ToLower(result)).Trim();

            return result;
        }

        /// <summary>
        /// Util service to parse account number from format XXXXXXº<AccountNumber>
        /// </summary>
        /// <param name="accountNumber">Account Number to be parsed</param>
        /// <returns>Parsed account number</returns>
        internal string ParseAccountNumber(string accountNumber)
        {
            return accountNumber.Replace(".", "").Replace("-", "");
        }


        /// <summary>
        /// Util service to Get Amount and Type. All amounts are converted to Positive Number, and Type is based on either the value was negative or positive
        /// </summary>
        /// <param name="amountString">Amount String with Sign</param>
        /// <param name="amount">Decimal conversion of the string amount, without the sign (out value)</param>
        /// <param name="type">Either Debt or Credit (from Constants) depending on sign of the value</param>
        internal void GetAmountAndType(string value, string description, out decimal amount, out int type)
        {
            if (value.StartsWith("-"))
            {
                if (description.StartsWith("TR-"))
                    type = Constants.TRF;
                else if (description.StartsWith("LEVANTAMENTO"))
                    type = Constants.LEV;
                else
                    type = Constants.TPA;
            }
            else
                type = Constants.CRD;
            amount = Convert.ToDecimal(value, Constants.CulturePT);
        }
    }
}
