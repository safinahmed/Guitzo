using System;
using System.Collections.Generic;
using System.IO;
using OffersLib;


namespace DataETLTest
{
    class GuitzoTest
    {
        public GuitzoTest() {}

        public void TestFileData(string filename)
        {
            Console.WriteLine("Starting...");

            byte[] bytes = null;
            FileStream fs = null;

            int correct = 0;
            int wrong = 0;
            int count = 0;
            try
            {

                fs = File.OpenRead(filename);
                bytes = new byte[fs.Length];
                fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                fs.Close();
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }

            try
            {
                DataETL dataETL = new DataETL();
                String entityName = "";
                FileDataRecord fileDataRecord = dataETL.ProcessUnknownData(bytes, out entityName);//dataETL.ProcessData(bytes, "BES", false);

                foreach (TransactionRecord tr in fileDataRecord)
                {
                    count++;
                    String parsedDescription = tr.ParsedDescription;
                    String description = tr.OriginalDescription;

                    Console.WriteLine(description + " - " + parsedDescription + " - " + tr.Type + " - " + tr.ExtraInfo);
                    Console.ReadKey();

                    //Console.WriteLine("***** -- " + parsedDescription + " -- ****");

                    /*Console.WriteLine("Prediction: " + PredictionEngine.Predict(parsedDescription,PredictionEngine.NAIVE_BAYES));
                    ConsoleKeyInfo cki = Console.ReadKey();
                    if (cki.KeyChar == 'y')
                        correct++;
                    else if(cki.KeyChar == 'n')
                        count--;*/

                    /*Console.WriteLine(PredictionEngine.Predict(parsedDescription, PredictionEngine.SAFIN));
                    Console.WriteLine(PredictionEngine.Predict(parsedDescription, PredictionEngine.SIMPLE_FULLTEXT));
                    Console.WriteLine("*********************");*/
                }
                Console.WriteLine("Correct: " +correct + " in " + count);

            }
            catch (ETLException etlException)
            {
                Console.WriteLine(etlException.Message);
            }

            Console.WriteLine("---FINISHED TEST FILE DATA---");
            Console.ReadLine();
        }

        public void TestOffers()
        {

            
            OffersCrawler oc = new OffersCrawler();
            List<Offer> list = oc.GetOffers(OffersCrawler.FORRETAS);

            foreach (var offer in list)
            {

                Console.WriteLine(offer.ToString());
               
            }

            Console.WriteLine("Finishing...");
            Console.ReadLine();

            //Test Program


            /*
            OffersCrawler oc = new OffersCrawler();
            List<Offer> list = oc.GetOffers(OffersCrawler.FORRETAS);
            Console.WriteLine("Finishing...");*/
        }
    }
}
