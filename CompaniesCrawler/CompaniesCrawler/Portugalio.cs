using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CompaniesCrawler
{
    class Portugalio
    {
        public Portugalio() {}

        public void CrawlCategories()
        {
            // Create web client simulating IE6.
            using (WebClient client = new WebClient())
            {
                client.Encoding = UTF8Encoding.UTF8;

                // Download data.
                byte[] arr = client.DownloadData("http://www.portugalio.com/empresas/");

                MemoryStream memoryStream = new MemoryStream(arr, false);

                HtmlDocument htmlDoc = new HtmlDocument();
                
                // There are various options, set as needed
                htmlDoc.OptionFixNestedTags = true;

                // filePath is a path to a file containing the html
                htmlDoc.Load(memoryStream,UTF8Encoding.UTF8);

                HtmlNodeCollection collection = htmlDoc.DocumentNode.SelectNodes("//li/a");

                int i = 0;
                foreach(HtmlNode node in collection)
                {
                    i++;
                   // Console.WriteLine(node.InnerText + " - " + node.Attributes[0].Value);
                    Connection.ExecuteNonQuery("INSERT INTO PT_CATEGORIAS VALUES("+ i + ",'" + node.InnerText + "','" +
                                               node.Attributes[0].Value + "','NEW','')");
                }

            }
        }

        public bool ProcessOneCategory()
        {
            int catId = 0;
            string catUrl = "";

            string url = "";
            try
            {
                SqlDataReader reader =
                    Connection.ExecuteReader("SELECT TOP 1 [ID], [NAME], [URL] FROM [companies].[dbo].[PT_CATEGORIAS] WHERE STATUS = 'NEW'");

                if (!reader.HasRows)
                    return false;

                reader.Read();
                catId = reader.GetInt32(0);
                catUrl = reader.GetString(2);
                reader.Close();


                int res = Connection.ExecuteNonQuery("UPDATE PT_CATEGORIAS SET STATUS = 'PROCESSING' WHERE ID = " + catId);
                if (res != 1)
                {
                    Console.WriteLine("ERROR UPDATING TO PROCESSING");
                }


                // Create web client simulating IE6.
                using (WebClient client = new WebClient())
                {
                    int count = 1;
                    bool hasNext = true;
                    while (hasNext)
                    {
                        client.Encoding = UTF8Encoding.UTF8;
                        // Download data.
                        url = "http://www.portugalio.com" + catUrl + count + ".html";
                        byte[] arr = client.DownloadData(url);

                        MemoryStream memoryStream = new MemoryStream(arr, false);

                        HtmlDocument htmlDoc = new HtmlDocument();
                        // There are various options, set as needed
                        htmlDoc.OptionFixNestedTags = true;

                        // filePath is a path to a file containing the html
                        htmlDoc.Load(memoryStream, UTF8Encoding.UTF8);

                        HtmlNodeCollection collection = htmlDoc.DocumentNode.SelectNodes("//a[@class='title']");

                        foreach (HtmlNode node in collection)
                        {
                            string nodeUrl = node.Attributes[1].Value;
                            if (nodeUrl.Length < 3 || nodeUrl.Equals("/img/") || nodeUrl.Equals("/casa-morais-turismo-rural/"))
                                continue;
                            ProcessOneEstablishment(nodeUrl,catId);
                        }

                        HtmlNode lastPage = htmlDoc.DocumentNode.SelectSingleNode("//a[@title='Última Página']");
                        if (lastPage == null)
                            hasNext = false;
                        else
                            count++;

                    }

                }

                res = Connection.ExecuteNonQuery("UPDATE PT_CATEGORIAS SET STATUS = 'FINISHED' WHERE ID = " + catId);
                if (res != 1)
                {
                    Console.WriteLine("ERROR UPDATING TO FINISHED");
                }
            }
            catch (Exception ex)
            {
                String message = ex.Message.Replace("'", "''");
                int res = Connection.ExecuteNonQuery("UPDATE PT_CATEGORIAS SET STATUS = 'ERROR', ERROR = '" + message + "' WHERE ID = " + catId);
                Trace.WriteLine("ERROR " + ex.Message + " " + url);
            }

            return true;
        }

        public void ProcessOneEstablishment(string establishmentUrl, int catId)
        {
            // Create web client simulating IE6.
            using (WebClient client = new WebClient())
            {
                client.Encoding = UTF8Encoding.UTF8;

                // Download data.
                byte[] arr = client.DownloadData("http://www.portugalio.com" + establishmentUrl);

                MemoryStream memoryStream = new MemoryStream(arr, false);

                HtmlDocument htmlDoc = new HtmlDocument();

                // There are various options, set as needed
                htmlDoc.OptionFixNestedTags = true;

                // filePath is a path to a file containing the html
                htmlDoc.Load(memoryStream, UTF8Encoding.UTF8);

                HtmlNode aNode = htmlDoc.DocumentNode.SelectSingleNode("//span[@class='name']");
                if (aNode == null)
                    return;
                string estName = aNode.InnerText.Replace("'","''").Replace("&amp;","&");

                string nif = "";
                string cae = "";

                aNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='companyExtra']");
                if (aNode != null)
                {
                    nif = findNIF(aNode.InnerText);

                    HtmlNode yaNode = aNode.SelectSingleNode("div");
                    if(yaNode != null)
                        processCae(yaNode.InnerText, out cae);
                } 

                
                int res = Connection.ExecuteNonQuery("INSERT INTO PT_ESTABELECIMENTOS VALUES('" + estName + "'," + catId + ",'" + cae + "','" + nif + "')" );
                if (res != 1)
                {
                    Console.WriteLine("ERROR INSERTING DATA");
                }
            }
        }

        public string findNIF(string inString)
        {
            string result = "";

            Regex rgx = new Regex("[0-9]{9}");
            Match m = rgx.Match(inString);
            if (m.Success)
                result = m.Value;

            return result;
        }

        public void processCae(string inString, out string caeNumber)
        {
            string[] split = inString.Split('-');
            caeNumber = split[0].Trim();
            string caeDesc = split[1].Replace(".", "").Trim();

            SqlDataReader reader =
    Connection.ExecuteReader("SELECT CAE FROM PT_CAE WHERE CAE = '" + caeNumber + "'");

            if (!reader.HasRows)
            {
                reader.Close();
                int res = Connection.ExecuteNonQuery("INSERT INTO PT_CAE VALUES('"+ caeNumber + "','" + caeDesc + "')");
                if (res != 1)
                {
                    Console.WriteLine("ERROR INSERTING CAE");
                }   
            }
            else
                reader.Close();
                
        }
        
    }
}
