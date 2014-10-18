using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CompaniesCrawler
{
    class BaseGeral
    {
        public BaseGeral()
        {
            
        }

        public void CrawlCAEs()
        {
            // Create web client simulating IE6.
            using (WebClient client = new WebClient())
            {
                // Download data.
                byte[] arr = client.DownloadData("http://www.base-geral.com/pt/empresas/cae-lista.asp");

                MemoryStream memoryStream = new MemoryStream(arr, false);

                HtmlDocument htmlDoc = new HtmlDocument();

                // There are various options, set as needed
                htmlDoc.OptionFixNestedTags = true;

                // filePath is a path to a file containing the html
                htmlDoc.Load(memoryStream);

                string nextPage = "";

                while(ProcessCaes(htmlDoc,out nextPage))
                {
                    arr = client.DownloadData("http://www.base-geral.com/pt/empresas/" + nextPage);
                    memoryStream = new MemoryStream(arr,false);
                    htmlDoc.Load(memoryStream);
                } 
            }
        }

        public bool ProcessCaes(HtmlDocument htmlDoc, out string nextPage)
        {
            bool result = false;

            nextPage = "";

            HtmlNodeCollection collection =
                        htmlDoc.DocumentNode.SelectNodes("//td[@width='100%']");

            HtmlNode node = collection[4];

            HtmlNodeCollection col = node.SelectNodes("table/tbody/tr");

            for(int i=1; i<col.Count; i++)
            {
                HtmlNode newNode = col[i];
                String cae = newNode.SelectSingleNode("td/a").InnerText;
                String caeDesc = newNode.SelectNodes("td")[1].InnerText.Replace("&nbsp;", "");

                int res = Connection.ExecuteNonQuery("INSERT INTO CAE VALUES('" + cae + "','" + caeDesc + "','NEW','')");
                if(res != 1)
                {
                    Console.WriteLine("ERROR");
                }

            }

            HtmlNode aNode = htmlDoc.DocumentNode.SelectSingleNode("//a[@title='Ver mais uma folha de resultados.']");
            if(aNode != null)
            {
                result = true;
                nextPage = aNode.Attributes[1].Value;
            }
            return result;
        }

        public bool ProcessOneCae()
        {
            string Cae = "";
            try
            {
                SqlDataReader reader =
                    Connection.ExecuteReader("SELECT TOP 1 [CAE] FROM [companies].[dbo].[CAE] WHERE [STATUS] = 'NEW'");

                if (!reader.HasRows)
                    return false;

                reader.Read();
                Cae = reader.GetString(0);
                reader.Close();


                int res = Connection.ExecuteNonQuery("UPDATE CAE SET STATUS = 'PROCESSING' WHERE CAE = '" + Cae + "'");
                if (res != 1)
                {
                    Console.WriteLine("ERROR");
                }


                // Create web client simulating IE6.
                using (WebClient client = new WebClient())
                {
                    // Download data.
                    string url = "http://www.base-geral.com/pt/empresas/empresas-cae.asp?cae=" + Cae;
                    byte[] arr = client.DownloadData(url);

                    MemoryStream memoryStream = new MemoryStream(arr, false);

                    HtmlDocument htmlDoc = new HtmlDocument();
                    // There are various options, set as needed
                    htmlDoc.OptionFixNestedTags = true;

                    // filePath is a path to a file containing the html
                    htmlDoc.Load(memoryStream);

                    HtmlNodeCollection collection = htmlDoc.DocumentNode.SelectNodes("//a[@class='three']");

                    foreach (HtmlNode node in collection)
                    {
                        CrawlEstabelecimento(node.Attributes[2].Value, Cae);
                    }

                }

                res = Connection.ExecuteNonQuery("UPDATE CAE SET STATUS = 'FINISHED' WHERE CAE = '" + Cae + "'");
                if (res != 1)
                {
                    Console.WriteLine("ERROR");
                }
            } catch(Exception ex)
            {
                int res = Connection.ExecuteNonQuery("UPDATE CAE SET STATUS = 'ERROR', ERROR = '"+ex.Message+"' WHERE CAE = '" + Cae + "'");
                if (res != 1)
                {
                    Console.WriteLine("ERROR");
                }
            }

            return true;
        }


        public void CrawlEstabelecimento(string url, string cae)
        {
            // Create web client simulating IE6.
            using (WebClient client = new WebClient())
            {
                // Download data.
                byte[] arr = client.DownloadData("http://www.base-geral.com/pt/empresas/" + url);

                MemoryStream memoryStream = new MemoryStream(arr, false);

                HtmlDocument htmlDoc = new HtmlDocument();

                // There are various options, set as needed
                htmlDoc.OptionFixNestedTags = true;

                // filePath is a path to a file containing the html
                htmlDoc.Load(memoryStream);

                string nextPage = "";

                while (processEstabelecimentos(htmlDoc, cae, out nextPage))
                {
                    arr = client.DownloadData("http://www.base-geral.com/pt/empresas/" + nextPage);
                    memoryStream = new MemoryStream(arr, false);
                    htmlDoc.Load(memoryStream);
                }
            }
        }


        public bool processEstabelecimentos(HtmlDocument htmlDoc, string cae, out string nextPage)
        {
            bool result = false;

            nextPage = "";

            HtmlNodeCollection collection =
                        htmlDoc.DocumentNode.SelectNodes("//td[@width='100%']");

            HtmlNode node = collection[4];

            HtmlNodeCollection col = node.SelectNodes("table/tbody/tr");

            for (int i = 1; i < col.Count; i++)
            {
                HtmlNode newNode = col[i];
                String estabelecimento = newNode.SelectNodes("td")[0].InnerText.Replace("&nbsp;", "").Replace("'","");
                String nif = newNode.SelectSingleNode("td/a").InnerText;


                int res = Connection.ExecuteNonQuery("INSERT INTO ESTABELECIMENTOS VALUES('" + estabelecimento + "','" + cae + "','"+nif+"')");
                if (res != 1)
                {
                    Console.WriteLine("ERROR");
                }
                 

            }

            HtmlNodeCollection aCol = node.SelectNodes("a");

            HtmlNode aNode = aCol[aCol.Count - 1];

            if (!aNode.InnerText.Contains("resultados"))
                return false;
            
            if (aNode != null)
            {
                result = true;
                nextPage = aNode.Attributes[0].Value;

                Regex rgx = new Regex("[0-9]+$");
                bool b = rgx.IsMatch(nextPage);
                if (b == false)
                    return false;
            }
             
            return result;
        }

    }
}
