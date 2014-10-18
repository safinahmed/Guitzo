using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Prediction;

namespace DataETLTest
{
    /// <summary>
    /// Connector to parse data from a BES Export
    /// </summary>
    public class BESConnector : IETLConnector
    {
        public FileDataRecord dataRecord;

        public BESConnector()
        {
            dataRecord =  new FileDataRecord();
        }

        public FileDataRecord Extract(byte[] fileData)
        {
            //In tuples, first value represents row, second represents column
            var accountLocation = new int[] { 0, 1 };
            var availableBalanceLocation = new int[] { 6, 1 }; 
            var accountingBalanceLocation = new int[] { 3, 1 }; 

            //Set known indexes
            const int dataStartRow = 10;
            const int dateIndex = 0;
            const int descriptionIndex = 3;
            const int debtIndex = 4;
            const int creditIndex = 5;
            const int typeIndex = 2;

            if (fileData == null)
            {
                throw new ETLException("fileData cannot be null");
            }


            try
            {
                MemoryStream memoryStream = new MemoryStream(fileData, false);

                XElement xDocument = XElement.Load(XmlReader.Create(memoryStream));

                var rows = xDocument.Descendants(Constants.ExcelNS + "Row");

                int i = 0;
                foreach (XElement xEle in rows)
                {
                    if (i == accountLocation[0])
                        dataRecord.AccountNumber =
                            xEle.Descendants(Constants.ExcelNS + "Cell").ElementAt(accountLocation[1]).Value;
                    if (i == accountingBalanceLocation[0])
                        dataRecord.AccountingBalance =
                            Convert.ToDecimal(
                                xEle.Descendants(Constants.ExcelNS + "Cell")
                                    .ElementAt(accountingBalanceLocation[1])
                                    .Value, Constants.CultureUS);
                    if (i == availableBalanceLocation[0])
                        dataRecord.AvailableBalance =
                            Convert.ToDecimal(
                                xEle.Descendants(Constants.ExcelNS + "Cell")
                                    .ElementAt(availableBalanceLocation[1])
                                    .Value, Constants.CultureUS);


                    if (i >= dataStartRow && i < rows.Count() - 1)
                    {
                        var cells = xEle.Descendants(Constants.ExcelNS + "Cell");

                        DateTime date = Convert.ToDateTime(cells.ElementAt(dateIndex).Value, Constants.CulturePT);
                        string sType = cells.ElementAt(typeIndex).Value;
                        string description = cells.ElementAt(descriptionIndex).Value;
                        string debt = cells.ElementAt(debtIndex).Value;
                        string credit = cells.ElementAt(creditIndex).Value;
                        decimal amount;
                        int type;

                        GetAmountAndType(debt, credit, sType, out amount, out type);

                        var etlRecord = new TransactionRecord(description, amount, date, type);

                        dataRecord.AddTransactionRecord(etlRecord);

                    }

                    i++;
                }
            }
            catch (Exception ex)
            {
                throw new ETLException("Could not extract BES Data: " + ex.Message);
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
                throw new ETLException("Could not transform BES Data: " + ex.Message);
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
                throw new ETLException("Could not categorize BES Data: " + ex.Message);
            }

            return dataRecord;
        }

        public void Load()
        {
            throw new NotImplementedException();
        }

        internal void GetAmountAndType(string debt, string credit, string sType, out decimal amount, out int type)
        {
            amount = 0;
            type = 0;

            if (!debt.Equals("-"))
            {
                amount = Convert.ToDecimal(debt, Constants.CultureUS);
            }
            else if (!credit.Equals("-"))
            {
                amount = Convert.ToDecimal(credit, Constants.CultureUS);
            }

            if (sType == "TPA")
                type = Constants.TPA;
            else if (sType == "PGT")
                type = Constants.PGT;
            else if (sType == "DEB")
                type = Constants.DEB;
            else if (sType == "TRF")
                type = Constants.TRF;
            else if (sType == "LEV")
                type = Constants.LEV;
            else if (sType == "SDD")
                type = Constants.SDD;
            else if (sType == "CRD")
                type = Constants.CRD;
            else if (sType == "STN")
                type = Constants.STN;
        }

        internal string ParseDescription(string description, out string extraInfo)
        {
            String result = description;
            extraInfo = "";

            bool sdd = false;

            //Remove initial COMPRA VISA CARTAO 2488707*
            Regex rgx = new Regex(@"COMPRA.*CARTAO [0-9]*\*");
            result = rgx.Replace(result, "").Trim();

            //Replace LEVANTAMENTO MB CARTAO 2488707*
            rgx = new Regex(@"LEVANTAMENTO.*CARTAO [0-9]*\*");
            result = rgx.Replace(result, "LEVANTAMENTO").Trim();

            //Remove everything after 3 spaces
            rgx = new Regex(@"\s\s\s+.*");
            result = rgx.Replace(result, "").Trim();

            //Replace CodPostal
            rgx = new Regex("[0-9]{4}-[0-9]?[0-9]?[0-9]?.*");
            result = rgx.Replace(result, "").Trim();

            //Remove **** **** 2488707*
            rgx = new Regex(@"(CARTAO\s+)?\*{4}\s+\*{4}\s+[0-9]{7}\*+");
            result = rgx.Replace(result, "").Trim();

            //Remove BESnet 530244888
            rgx = new Regex("BESnet [0-9]{9}");
            result = rgx.Replace(result, "").Trim();

            //Remove NIBs
            //rgx = new Regex("[0-9]{21}");
            //result = rgx.Replace(result, "").Trim();

            //Case: Pagamento Prestacao Ch N.  52 Processo 0911001947 (remove Ch and following)
            rgx = new Regex(@"CH N.\s+[0-9]+\s+PROCESSO\s+[0-9]*");
            result = rgx.Replace(result, "").Trim();


            if (result.StartsWith("CARREG"))
                result = description.Split(' ')[1];

            //Case: COBRANCA IDD 103094 GDL CUR          N. ADC 80073748988
            if(result.StartsWith("COBRANCA IDD "))
            {
                sdd = true;
                rgx = new Regex("COBRANCA IDD [0-9]*");
                result = rgx.Replace(result, "").Trim();

                //Try to get the DD number (103094 in case)
                Match match = rgx.Match(description);
                if (match.Success)
                    extraInfo = match.Value.Replace("COBRANCA IDD ", "").Trim();

                //Remove ADC and number
                rgx = new Regex("N. ADC [0-9]*");
                result = rgx.Replace(result, "").Trim();  
            }

            //Case: Cobranca Cdc El Corte Ingles Referencia 9503030869
            if (result.StartsWith("COBRANCA CDC "))
            {
                result = result.Replace("COBRANCA CDC","").Trim();

                rgx = new Regex("REFERENCIA [0-9]*");
                result = rgx.Replace(result, "").Trim();
            }

            //Case: Pag Servicos **** **** 2488707* 20843 Ref 530244888
            if (result.StartsWith("PAG SERVICOS "))
            {
                //Remove Ref 530244888
                rgx = new Regex("REF [0-9]{9}");
                result = rgx.Replace(result, "").Trim();

                //Catch and remove Entity
                rgx = new Regex("[0-9]{5}");
                Match match = rgx.Match(result);
                if (match.Success)
                    extraInfo = match.Value;

                String name = PagamentoServicos.Get(extraInfo);
                if (!string.IsNullOrEmpty(name))
                    result = name;
                //result = rgx.Replace(result, "").Trim();
            }

            //Case: Pag Serv Besnet 134910077 Entidade 10147
            if (result.StartsWith("PAG SERV ") || result.StartsWith("PAG COMPRAS "))
            {
                result = result.Replace("ENTIDADE", "").Trim();

                //Catch and remove Entity
                rgx = new Regex("[0-9]{5}");
                Match match = rgx.Match(result);
                if (match.Success)
                    extraInfo = match.Value;

                String name = PagamentoServicos.Get(extraInfo);
                if (!string.IsNullOrEmpty(name))
                    result = name;
                //result = rgx.Replace(result, "").Trim();
            }

            //Case: PAG SEG SOCIAL BESnet 136302648 12041165576 201210
            if (result.StartsWith("PAG SEG SOCIAL "))
            {
                result = "PAG SEG SOCIAL";
                //Catch Date
                rgx = new Regex("[0-9]{6}");
                Match match = rgx.Match(result);
                if (match.Success)
                    result = result + " " + match.Value;
            }

            //rgx = new Regex("[0-9]{4}\\s+\\w+");
            //result = rgx.Replace(result, "").Trim();

            result = Utils.CleanCompanyName(result);

            if(sdd && !String.IsNullOrEmpty(extraInfo))
            {
                SDD.Put(extraInfo,result);
            }

            result = Constants.TextInfo.ToTitleCase(Constants.TextInfo.ToLower(result)).Trim();

            return result;
        }

    }
}
