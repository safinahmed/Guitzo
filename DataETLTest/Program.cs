using System;
using System.Collections.Generic;
using System.IO;
using OffersLib;
using Prediction;

namespace DataETLTest
{
    class Program
    {

        static void Main(string[] args)
        {
            //TEST FILES DATA
            GuitzoTest gt = new GuitzoTest();
            //gt.TestFileData(@"C:\Users\sahmed\Dropbox\MRKL\File Samples\BES\20120923 Saldos e Movimentos 0000 7545 8629 Original.xls");
            //TEST OFFERS
            //gt.TestOffers();

            //TEST PREDICTOR
            //SampleTests st = new SampleTests();
            //st.Test(@"C:\Guitzo\Files Data\TestingData.csv", "BES");

            //TEST READING / PARSING OF FILES WITH DB DATA
            //TestSamples ts = new TestSamples();
            //ts.Load(@"C:\Users\sahmed\Dropbox\MRKL\File Samples\DataBase Data Samples\BetaSample.csv", "BPI");

            //TEST PREDICTOR WITH READING / PARSING
            //ts.Predict();

            //GATHER HOT WORDS
            //HotWords hw = new HotWords();
            //hw.GatherRatioWords();
            
        }
    }
}
