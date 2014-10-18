using System;
using System.IO;
using System.Text;
using HtmlAgilityPack;
using Prediction;


namespace DataETLTest
{
    /// <summary>
    /// Connector to parse data from a BES Export
    /// </summary>
    class BestConnector : IETLConnector
    {

        private FileDataRecord dataRecord;

        public BestConnector()
        {
            dataRecord = new FileDataRecord();
        }

        public FileDataRecord Extract(byte[] fileData)
        {
            if (fileData == null)
            {
                throw new ETLException("fileData cannot be null");
            }

            try
            {
                MemoryStream memoryStream = new MemoryStream(fileData, false);

                HtmlDocument htmlDoc = new HtmlDocument();

                // There are various options, set as needed
                htmlDoc.OptionFixNestedTags = true;

                // filePath is a path to a file containing the html
                htmlDoc.Load(memoryStream, UTF8Encoding.UTF8);


                if (htmlDoc.DocumentNode != null)
                {

                    HtmlNode aNode =
                        htmlDoc.DocumentNode.SelectSingleNode("//tbody[@class='jQOddEvenZone']");

                    HtmlNodeCollection aCol = aNode.SelectNodes("tr");

                    foreach (HtmlNode node in aCol)
                    {
                        HtmlNodeCollection yaCol = node.SelectNodes(".//span");

                        DateTime date = Convert.ToDateTime(yaCol[0].InnerText, Constants.CulturePT);

                        string description = yaCol[3].InnerText;
                        string alternateDescription = yaCol[2].InnerText;

                        string amountString = yaCol[5].InnerText;
                        string signal = yaCol[4].InnerText;

                        decimal amount;
                        int type;

                        GetAmountAndType(amountString, alternateDescription, signal, out amount, out type);

                        var etlRecord = new TransactionRecord(description, amount, date, type);

                        dataRecord.AddTransactionRecord(etlRecord);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ETLException("Could not extract BEST Data: " + ex.Message);
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
                throw new ETLException("Could not transform BEST Data: " + ex.Message);
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
                throw new ETLException("Could not categorize BEST Data: " + ex.Message);
            }

            return dataRecord;
        }

        private string ParseDescription(string description)
        {
            String result = description;

            result = Utils.CleanCompanyName(result);

            result = Constants.TextInfo.ToTitleCase(Constants.TextInfo.ToLower(result)).Trim();

            return result;
        }

        public void Load()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Util service to Get Amount and Type
        /// </summary>
        /// <param name="amountString">Amount String with Sign</param>
        /// <param name="amount">Decimal conversion of the string amount, without the sign (out value)</param>
        /// <param name="type">Either Debt or Credit (from Constants) depending on sign of the value</param>
        internal void GetAmountAndType(string amountString, string description, string signal, out decimal amount, out int type)
        {
            //D means Debit, so, we have to put the sign
            if (signal == "D")
                amountString = "-" + amountString;

            if (description.Equals("TRANSFERÊNCIAS"))
                type = Constants.TRF;
            else if (description.Equals("CRÉDITOS"))
                type = Constants.CRD;
            else if (description.Equals("DÉBITOS"))
                type = Constants.DEB;
            else
                type = Constants.TPA;
            amount = Convert.ToDecimal(amountString, Constants.CulturePT);
        }
    }
}
