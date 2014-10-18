using System;
using Prediction;

namespace DataETLTest
{
    /// <summary>
    /// Connector to parse data from a CGD Export
    /// </summary>
    public class CGDConnector : IETLConnector
    {
        public FileDataRecord dataRecord;

        public CGDConnector()
        {
            dataRecord =  new FileDataRecord();
        }

        public FileDataRecord Extract(byte[] fileData)
        {
            //Set known indexes
            const int accountNumberIndex = 1;
            const int balanceIndex = 6;
            const int dateIndex = 0;
            const int descriptionIndex = 2;
            const int debtIndex = 3;
            const int creditIndex = 4;

            if (fileData == null)
            {
                throw new ETLException("fileData cannot be null");
            }

            try
            {
                var csv = new CSVUtil(fileData, ";");

                //Skip to Account Data
                csv.SkipLines(2);
                csv.Next();

                string accountNumber = csv[accountNumberIndex];
                dataRecord.AccountNumber = ParseAccountNumber(accountNumber);

                //Skip to Transaction Data
                csv.SkipLines(4);

                while (csv.Next())
                {
                    //If there is no date, file is over
                    if (csv[dateIndex] == "")
                        break;

                    DateTime date = Convert.ToDateTime(csv[dateIndex], Constants.CulturePT);
                    string description = csv[descriptionIndex];
                    string debt = csv[debtIndex];
                    string credit = csv[creditIndex];
                    decimal amount;
                    int type;

                    GetAmountAndType(debt, credit, description, out amount, out type);

                    var etlRecord = new TransactionRecord(description, amount, date, type);

                    dataRecord.AddTransactionRecord(etlRecord);
                }

                csv.Next();
                string availableBalance = csv[balanceIndex];
                dataRecord.AvailableBalance = ConvertBalance(availableBalance);

                csv.Next();
                string accountingBalance = csv[balanceIndex];
                dataRecord.AccountingBalance = ConvertBalance(accountingBalance);
            }
            catch (Exception ex)
            {
                throw new ETLException("Could not extract CGD Data: " + ex.Message);
            }

            return dataRecord;
        }

        public FileDataRecord Transform()
        {
            try
            {
                foreach (TransactionRecord tr in dataRecord)
                {
                    tr.ParsedDescription = ParseDescription(tr.OriginalDescription);
                }
            }
            catch (Exception ex)
            {
                throw new ETLException("Could not transform CGD Data: " + ex.Message);
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
                throw new ETLException("Could not categorize CGD Data: " + ex.Message);
            }

            return dataRecord;
        }

        public void Load()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Util service to parse account number from format <AccountNumber>-XXXXXXX
        /// </summary>
        /// <param name="accountNumber">Account Number to be parsed</param>
        /// <returns>Parsed account number</returns>
        internal string ParseAccountNumber(string accountNumber)
        {
            return accountNumber.Split('-')[0].Trim();
        }

        internal string ParseDescription(string description)
        {
            String result = description.Replace("COMPRA ","");

            result = Utils.CleanCompanyName(result);

            result = Constants.TextInfo.ToTitleCase(Constants.TextInfo.ToLower(result)).Trim();
            //Provavelmente mais instruções

            return result;
        }

        /// <summary>
        /// Util service to convert balance from format <Balance> EUR, removing EUR and converting to decimal
        /// </summary>
        /// <param name="balance">Balance to be converted</param>
        /// <returns>Converted Balance</returns>
        internal decimal ConvertBalance(string balance)
        {
            return Convert.ToDecimal(balance.Split(' ')[0].Trim(),Constants.CulturePT);
        }

        internal void GetAmountAndType(string debt, string credit, string description, out decimal amount, out int type)
        {
            amount = 0;
            type = 0;

            description = description.ToUpper(Constants.CulturePT);

            if (!debt.Equals(""))
            {
                debt = "-" + debt;
                amount = Convert.ToDecimal(debt, Constants.CulturePT);
                if (description.StartsWith("PAG SERV") || description.StartsWith("CARREGAMENTO"))
                    type = Constants.PGT;
                else if (description.StartsWith("LEVANTAMENTO"))
                    type = Constants.LEV;
                else if (description.StartsWith("TRF"))
                    type = Constants.TRF;
                else
                    type = Constants.TPA;
            }
            else if (!credit.Equals(""))
            {
                amount = Convert.ToDecimal(credit, Constants.CulturePT);
                type = Constants.CRD;
            }
        }
    }
}
