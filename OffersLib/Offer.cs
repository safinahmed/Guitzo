using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OffersLib
{
    public class Offer
    {
        public String URL { get; set; }
        public String Title { get; set; }
        public String Supplier { get; set; }
        public String Category { get; set; }
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal SavingAmount { get; set; }
        public decimal SavingPercentage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public String Description { get; set; }

        public override string ToString()
        {
            string result = "[title: " + Title + ", price: " + Price + ", originalPrice: " + OriginalPrice + ", savingAmount: " + SavingAmount + ", savingPercentage: " + SavingPercentage + "]";
            return result;
        }
    }
}
