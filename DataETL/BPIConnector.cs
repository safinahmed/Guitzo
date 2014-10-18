using System;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Prediction;

namespace DataETLTest
{
    /// <summary>
    /// Connector to parse data from a BPI Export
    /// </summary>
    public class BPIConnector : IETLConnector
    {
        public FileDataRecord dataRecord;

        public BPIConnector()
        {
            dataRecord =  new FileDataRecord();
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
                htmlDoc.Load(memoryStream);


                if (htmlDoc.DocumentNode != null)
                {

                    HtmlNodeCollection collection =
                        htmlDoc.DocumentNode.SelectNodes(
                            "//font[@style='FONT-SIZE:10px; COLOR:#666666; FONT-FAMILY:Verdana']/b");

                    if (collection != null)
                        dataRecord.AccountNumber = ParseAccountNumber(collection[1].InnerText);

                    collection = htmlDoc.DocumentNode.SelectNodes(
                        "//td[@class='xl24']/b");

                    if (collection != null)
                    {
                        dataRecord.AvailableBalance = ParseBalance(collection[0].InnerText);
                        dataRecord.AccountingBalance = ParseBalance(collection[1].InnerText);
                    }


                    collection = htmlDoc.DocumentNode.SelectNodes(
                        "//tr[@valign='top']");

                    foreach (HtmlNode node in collection)
                    {
                        HtmlNodeCollection transactionCollection = node.SelectNodes("td[@class='xl26']");

                        DateTime date = DateTime.MinValue;

                        if (transactionCollection != null)
                            date = Convert.ToDateTime(transactionCollection[0].InnerText, Constants.CulturePT);

                        HtmlNode descriptionNode = node.SelectSingleNode("td/font");

                        string description = "";

                        if (descriptionNode != null)
                            description = descriptionNode.InnerText.Trim();

                        transactionCollection = node.SelectNodes("td[@class='xl24']");

                        string amountString = "";

                        if (transactionCollection != null)
                            amountString = transactionCollection[0].InnerText;

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
                throw new ETLException("Could not extract BPI Data: " + ex.Message);
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
                throw new ETLException("Could not transform BPI Data: " + ex.Message);
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
                throw new ETLException("Could not categorize BPI Data: " + ex.Message);
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

            //Remove initial 16/09
            Regex rgx = new Regex("^[0-9]{2}/[0-9]{2}");
            result = rgx.Replace(result, "").Trim();

            //Remove initial COMPRA ELEC 4011676/01
            rgx = new Regex("^COMPRA.*[0-9]{7}/[0-9]{2}");
            result = rgx.Replace(result, "").Trim();

            //Replace initial 16/09 LEV. ATM 4011676/01 to LEVANTAMENTO
            rgx = new Regex("^LEV.*[0-9]{7}/[0-9]{2}");
            result = rgx.Replace(result, "LEVANTAMENTO").Trim();

            //Replace CodPostal
            rgx = new Regex("[0-9]{4}-[0-9]?[0-9]?[0-9]?.*");
            result = rgx.Replace(result, "").Trim();

            //Remove everything after 3 spaces
            rgx = new Regex(@"\s\s\s+.*");
            result = rgx.Replace(result, "").Trim();

            //Case: PAG. SERV. ATM REF. VODAF0046 IM PENICHE
            result = result.Replace("PAG. SERV. ATM REF. ", "").Trim();

            //Replace PAG.A XXX SDD xxxxxx to XXX
            if (result.StartsWith("PAG.A"))
            {
                result = result.Replace("PAG.A ", "").Trim();
                result = result.Replace("PAG.AUT.", "").Trim();
                rgx = new Regex("SDD [0-9]*");
                result = rgx.Replace(result, "").Trim();
            }

            //Case: Trf 0000076 P/ 4372246.000.001 Filipe Dias
            if(result.StartsWith("TRF"))
            {
                rgx = new Regex(@"[0-9]+\.[0-9]+\.[0-9]+\.?[0-9]*");
                Match m = rgx.Match(description);
                if(m.Success)
                    extraInfo = m.Value.Replace(".", "").Trim();

                result = rgx.Replace(result, "").Trim();

                rgx = new Regex("[0-9]{7}");
                result = rgx.Replace(result, "").Trim();
            }

            //Case: Cob.Sdd Quotas Auto Club Portug Ref 00045261719
            if(result.StartsWith("COB.SDD"))
            {
                result = result.Replace("COB.SDD ", "").Trim();
                rgx = new Regex("REF [0-9]{11}");
                result = rgx.Replace(result, "").Trim();
            }

            //Case: PAGAMENTO SERVICOS INTERNET - 70116652
            if(result.StartsWith("PAGAMENTO SERVICOS"))
            {
                rgx = new Regex("[0-9]{8}");
                result = rgx.Replace(result, "").Trim();
            }

            //Case: 21/10 PAG. PORTAGEM/TELEF. PUBL. ELEC 3251702/25
            if (result.StartsWith("PAG."))
            {
                result = result.Replace("PAG. ", "").Trim();
                rgx = new Regex(@"\w+\s[0-9]+/[0-9]+");
                result = rgx.Replace(result, "").Trim();
            }

            result = Utils.CleanCompanyName(result);

            result = Constants.TextInfo.ToTitleCase(Constants.TextInfo.ToLower(result)).Trim();

            return result;
        }

        internal string ParseAccountNumber(string accountNumber)
        {
            string result = accountNumber;

            if (accountNumber != null)
                result = accountNumber.Split('&')[0].Replace("-","").Replace(".","");

            return result;
        }

        internal decimal ParseBalance(string balance)
        {
            decimal result = 0;

            if (balance != null)
                result = Convert.ToDecimal(balance.Split('&')[0],Constants.CulturePT);

            return result;
        }

        internal void GetAmountAndType(string amountString, string description, out decimal amount, out int type)
        {

            amountString = amountString.Split('&')[0];

            if (amountString.StartsWith("-"))
            {
                if (description.Contains("SDD"))
                    type = Constants.SDD;
                else if (description.StartsWith("TRF"))
                    type = Constants.TRF;
                else if (description.Contains("PAG. SERV."))
                    type = Constants.PGT;
                else if (description.Contains("LEV."))
                    type = Constants.LEV;
                else
                    type = Constants.TPA;                
            }
            else
                type = Constants.CRD;
            amount = Convert.ToDecimal(amountString, Constants.CulturePT);
        }
    }
}
