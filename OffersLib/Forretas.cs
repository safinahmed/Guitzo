using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DataETLTest;
using HtmlAgilityPack;

namespace OffersLib
{
    class Forretas
    {
        CultureInfo pt = CultureInfo.CreateSpecificCulture("pt-PT");

        public Forretas()
        {
            
        }

        internal List<Offer> CrawlOffers()
        {

            
            List<Offer> result = new List<Offer>();

            try
            {
                // Create web client simulating IE6.
                using (WebClient client = new WebClient())
                {


                    client.Encoding = UTF8Encoding.UTF8;

                    for (int cnt = 1; cnt < 17; cnt++)
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

                        foreach (HtmlNode node in hnc)
                        {
                            String url = node.SelectSingleNode(".//div[@class='titleDeal']/a").Attributes[0].Value;
                            url = "http://www.forretas.com" + url;
                            String title =
                                node.SelectSingleNode(".//div[@class='titleDeal']/a").Attributes[2].Value.Replace("'",
                                                                                                                  "''");
                            if (title.Trim().Length < 10)
                                title =
                                    node.SelectSingleNode(".//div[@class='titleDeal']/a")
                                        .InnerText.Trim()
                                        .Replace("'", "''");
                            String supplier =
                                node.SelectNodes(".//p[@class='dealfrom']")[0].SelectSingleNode(".//strong")
                                                                              .InnerText.Trim();
                            String categoria = node.SelectSingleNode(".//span[@class='labelCat']/a").InnerText.Trim();
                            categoria = convertCategory(categoria);
                            String price =
                                node.SelectSingleNode(".//li[@class='discount']")
                                    .InnerText.Trim()
                                    .Replace("ver oferta", "")
                                    .Replace("€", "");
                            String originalAmount =
                                node.SelectSingleNode(".//li[@class='details']/span/label")
                                    .InnerText.Trim()
                                    .Replace("€", "");
                            String savingAmount =
                                node.SelectSingleNode(".//li[@class='details']/span[3]/strong")
                                    .InnerText.Trim()
                                    .Replace("€", "");
                            String savingPer =
                                node.SelectSingleNode(".//li[@class='details']/span[2]/strong")
                                    .InnerText.Trim()
                                    .Replace("%", "");

                            String endDate = node.SelectSingleNode(".//li[@class='time']").InnerText.Trim();
                            DateTime endDateDT = parseEndDate(endDate);

                            Offer offer = new Offer();
                            offer.URL = url;
                            offer.Title = title;
                            offer.Supplier = supplier;
                            offer.Category = categoria;
                            offer.Price = Convert.ToDecimal(price, pt);
                            offer.OriginalPrice = Convert.ToDecimal(originalAmount, pt);
                            offer.SavingAmount = Convert.ToDecimal(savingAmount, pt);
                            offer.SavingPercentage = Convert.ToDecimal(savingPer, pt);
                            offer.EndDate = endDateDT;

                            result.Add(offer);

                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.log("Error getting Offers " + ex.Message,"OffersLib");
            }
            return result;
        }

        private DateTime parseEndDate(String endDate)
        {
            DateTime dt = DateTime.Now;
            try
            {
                String date = "";
                if (endDate == null || endDate.Equals("Terminado"))
                    return DateTime.Now;

                Regex rgx = new Regex("[0-9]+:[0-9]+");
                Match m = rgx.Match(endDate);
                if (m.Success)
                    date = m.Value;



                String hours = date.Split(':')[0];
                String minutes = date.Split(':')[1];

                dt = dt.AddHours(Convert.ToDouble(hours, pt));
                dt = dt.AddMinutes(Convert.ToDouble(minutes, pt));
            }
            catch (Exception ex)
            {
                Logger.log("Error parsingEndDate " + ex.Message, "OffersLib");
            }

            return dt;

        }

        private String convertCategory(String categoria)
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
