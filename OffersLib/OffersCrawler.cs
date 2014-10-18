using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OffersLib
{
    public class OffersCrawler
    {
        public const int FORRETAS = 1;

        public OffersCrawler()
        {
            
        } 

        public List<Offer> GetOffers(int source)
        {
            List<Offer> result = new List<Offer>();
            if(source.Equals(FORRETAS))
            {
                Forretas forretas = new Forretas();
                result = forretas.CrawlOffers();
            }
            return result;
        } 
    }
}
