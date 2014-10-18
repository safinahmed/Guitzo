using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CompaniesCrawler
{
    class Forretas
    {
        public Forretas()
        {
            
        }

        public void CrawlOffers()
        {
            // Create web client simulating IE6.
            using (WebClient client = new WebClient())
            {


                client.Encoding = UTF8Encoding.UTF8;

                for(int cnt = 1; cnt < 100; cnt++)
                {
                    // Download data.
                    byte[] arr = client.DownloadData("http://forretas.com/deals.aspx?c=lisboa&pg=" + cnt);

                    MemoryStream memoryStream = new MemoryStream(arr, false);

                    HtmlDocument htmlDoc = new HtmlDocument();

                    // There are various options, set as needed
                    htmlDoc.OptionFixNestedTags = true;

                    // filePath is a path to a file containing the html
                    htmlDoc.Load(memoryStream, UTF8Encoding.UTF8);

                    HtmlNodeCollection hnc = htmlDoc.DocumentNode.SelectNodes("//div[@class='deal']");

                    if (hnc == null)
                        break;

                    foreach (HtmlNode node in hnc)
                    {
                        String url = node.SelectSingleNode(".//div[@class='titleDeal']/a").Attributes[0].Value;
                        url = "http://www.forretas.com" + url;
                        String title = node.SelectSingleNode(".//div[@class='titleDeal']/a").Attributes[2].Value.Replace("'", "''");
                        if(title.Trim().Length < 10)
                            title = node.SelectSingleNode(".//div[@class='titleDeal']/a").InnerText.Trim().Replace("'","''");
                        String supplier =
                            node.SelectNodes(".//p[@class='dealfrom']")[0].SelectSingleNode(".//strong").InnerText.Trim();
                        String categoria = node.SelectSingleNode(".//span[@class='labelCat']/a").InnerText.Trim();
                        categoria = convertCategory(categoria);
                        String price =
                            node.SelectSingleNode(".//li[@class='discount']").InnerText.Trim().Replace("ver oferta", "").Replace("€","");
                        String savingAmt = node.SelectSingleNode(".//li[@class='details']/span/label").InnerText.Trim().Replace("€","");
                        String savingPer = node.SelectSingleNode(".//li[@class='details']/span/strong").InnerText.Trim().Replace("%","");

                        String startDate = "";
                        String endDate = node.SelectSingleNode(".//li[@class='time']").InnerText.Trim();
                        endDate = parseEndDate(endDate);
                        String insertString = "INSERT INTO OFERTAS VALUES('" + categoria + "','" + supplier + "','" +
                                              title + "','" + startDate + "','" + endDate + "','" + url + "','" +
                                              savingAmt + "','" + savingPer + "','" + price + "')";

                        Connection.ExecuteNonQuery(insertString);

                    }
                }

            }
        }

        public String parseEndDate(String endDate)
        {
            String date ="";
            if (endDate == null)
                return "";

            Regex rgx = new Regex("[0-9]+:[0-9]+");
            Match m = rgx.Match(endDate);
            if (m.Success)
                date = m.Value;

            DateTime dt = DateTime.Now;

            String hours = date.Split(':')[0];
            String minutes = date.Split(':')[1];

            dt = dt.AddHours(Convert.ToDouble(hours));
            dt = dt.AddMinutes(Convert.ToDouble(minutes));

            return dt.ToShortDateString();

        }

        public String convertCategory(String categoria)
        {
            if (categoria.Equals("Produtos e Serviços"))
                return "Shopping";
            if (categoria.Equals("Saúde e Beleza"))
                return "Saúde e Bem-Estar";
            if (categoria.Equals("Entretenimento"))
                return "Lazer e Entretenimento";
            if (categoria.Equals("Turismo"))
                return "Hotéis e Viagens";
            if (categoria.Equals("Restaurantes"))
                return "Restaurante";
            if (categoria.Equals("Desporto e Fitness"))
                return "Saúde e Bem-Estar";
            if (categoria.Equals("Cursos e Formação"))
                return "Ensino e Formação";
            return categoria;
        }
    }
}
