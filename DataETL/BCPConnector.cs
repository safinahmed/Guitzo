using System;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Prediction;


namespace DataETLTest
{
    /// <summary>
    /// Connector to parse data from a BES Export
    /// </summary>
    public class BCPConnector : IETLConnector
    {

        public FileDataRecord dataRecord;

        public BCPConnector()
        {
            dataRecord =  new FileDataRecord();
        }

        public FileDataRecord Extract(byte[] fileData)
        {
            int dataStartIndex = 13; //13 empty rows

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
                htmlDoc.Load(memoryStream);


                if (htmlDoc.DocumentNode != null)
                {

                    HtmlNode aNode =
                        htmlDoc.DocumentNode.SelectSingleNode(
                            "//td[@align='left']");

                    dataRecord.AccountNumber = ParseAccountNumber(aNode.Attributes[3].Value);


                    HtmlNodeCollection collection = htmlDoc.DocumentNode.SelectNodes(
                        "//td[@class='DOUBLE2']");

                    dataRecord.AvailableBalance = Convert.ToDecimal(collection[0].Attributes[2].Value,
                                                                    Constants.CultureUS);
                    dataRecord.AccountingBalance = Convert.ToDecimal(collection[1].Attributes[2].Value,
                                                                     Constants.CultureUS);


                    collection = htmlDoc.DocumentNode.SelectNodes("//tr");

                    int counter = 0;
                    foreach (HtmlNode node in collection)
                    {

                        if (counter++ < dataStartIndex)
                            continue;

                        if (counter == collection.Count)
                            break;

                        HtmlNode yaNode = node.SelectSingleNode("td[@class='DATA']");

                        double dateDouble = Convert.ToDouble(yaNode.Attributes[2].Value, Constants.CultureUS);
                        DateTime date = DateTime.FromOADate(dateDouble);

                        HtmlNode descriptionNode = node.SelectSingleNode("td[@align='left']");

                        string description = descriptionNode.Attributes[1].Value;

                        yaNode = node.SelectSingleNode("td[@class='DOUBLE2']");

                        string amountString = yaNode.Attributes[2].Value;

                        decimal amount;
                        int type;

                        GetAmountAndType(amountString, description, out amount, out type);

                        var etlRecord = new TransactionRecord(description, amount, date, type);

                        dataRecord.AddTransactionRecord(etlRecord);

                    }
                }
            }
            catch (Exception ex)
            {
                throw new ETLException("Could not parse BCP Extract File " + ex.Message);
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
                throw new ETLException("Could not Transform BCP Data: " + ex.Message);
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
                throw new ETLException("Could not categorize BCP Data: " + ex.Message);    
            }
            return dataRecord;
        }

        private string ParseDescription(string description, out string extraInfo)
        {
            String result = description;
            extraInfo = "";

            bool sdd = false;
            
            //If we have a -, keep only the second part (NEED TO CONFIRM)
            if (result.Contains("-"))
                result = result.Split('-')[1];

            //Remove Extra Spaces
            Regex rgx = new Regex("\\s\\s+");
            result = rgx.Replace(result, " ").Trim();

            //Remove MMD3751942
            rgx = new Regex("MMD[0-9]*\\s");
            result = rgx.Replace(result, "").Trim();

            //Remove Firt Part of DD100919OCIDENTAL SEGUROS 03138678955 20120430
            rgx = new Regex("DD[0-9]*");
            result = rgx.Replace(result, "").Trim();

            //Check for DD ID
            Match match = rgx.Match(description);
            if (match.Success)
            {
                sdd = true;
                extraInfo = match.Value.Replace("DD", "").Trim();
                //Remove Dates
                rgx = new Regex("[0-9]*$");
                result = rgx.Replace(result, "").Trim();
            }

            //Remove NIBs
            rgx = new Regex("[0-9]{20}\\s?");
            result = rgx.Replace(result, "").Trim();

            if (!result.StartsWith("TRF"))
            {
                //Remove Account Numbers / ADC
                rgx = new Regex("[0-9]{11}\\s?");
                result = rgx.Replace(result, "").Trim();
            }

            //Remove MOV XX
            rgx = new Regex("MOV\\s[0-9]{2}");
            result = rgx.Replace(result, "").Trim();

            result = Utils.CleanCompanyName(result);

            //If it's DD and has extraInfo, try to teach - Remove in the future
            if(sdd && !String.IsNullOrEmpty(extraInfo))
            {
                SDD.Put(extraInfo,result);
            }

            result = Constants.TextInfo.ToTitleCase(Constants.TextInfo.ToLower(result)).Trim();

            return result;
        }

        public void Load()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Util service to parse account number from format XXXXXXºAccountNumber
        /// </summary>
        /// <param name="accountNumber">Account Number to be parsed</param>
        /// <returns>Parsed account number</returns>
        internal string ParseAccountNumber(string accountNumber)
        {
            return accountNumber.Split('º')[1].Trim();
        }


        /// <summary>
        /// Util service to Get Amount and Type. All amounts are converted to Positive Number, and Type is based on either the value was negative or positive
        /// </summary>
        /// <param name="amountString">Amount String with Sign</param>
        /// <param name="amount">Decimal conversion of the string amount, without the sign (out value)</param>
        /// <param name="type">Either Debt or Credit (from Constants) depending on sign of the value</param>
        internal void GetAmountAndType(string amountString, string description, out decimal amount, out int type)
        {
            if (amountString.StartsWith("-"))
            {
                if (description.StartsWith("DD"))
                    type = Constants.SDD;
                else if (description.StartsWith("TRF"))
                    type = Constants.TRF;
                else if (description.Contains("LVT"))
                    type = Constants.LEV;
                else
                    type = Constants.TPA;
            }
            else
            {
                if (description.StartsWith("Trf"))
                    type = Constants.TRF;
                else
                    type = Constants.CRD;
            }
            amount = Convert.ToDecimal(amountString,Constants.CultureUS);
        }
    }
}
