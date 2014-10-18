using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CompaniesCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            //Portugalio pt = new Portugalio();
            //while(pt.ProcessOneCategory());
            // Console.ReadKey();
            Forretas f = new Forretas();
            f.CrawlOffers();

        }
    }
}
