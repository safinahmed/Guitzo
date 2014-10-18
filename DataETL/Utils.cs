using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DataETLTest
{
    /// <summary>
    /// Class that will contain all Util services
    /// Needs to be in a higher hierarchy level (not inside ETL only)
    /// </summary>
    static class Utils
    {
        public static void GetAmountAndType(string  debt, string credit, CultureInfo culture, out decimal amount, out int type)
        {
            amount = 0;
            type = 0;

            //Fix Locale decimal separator
            debt = debt.Replace(".",",");
            credit = credit.Replace(".", ",");

            if (!debt.Equals(""))
            {
                amount = Convert.ToDecimal(debt,culture);
                type = Constants.Debt;
            }
            else if (!credit.Equals(""))
            {
                amount = Convert.ToDecimal(credit,culture);
                type = Constants.Credit;
            }
        }

        public static string CleanCompanyName(string company)
        {
            string result = company.ToUpper();

            result = result.Replace(",", " ");
            result = result.Replace("-", " ");
            result = result.Replace(" LDA", " "); //Remove LDA
            result = Regex.Replace(result, @" S A(\s|$)", " "); //Remove S A 
            result = Regex.Replace(result, @" UNIP(\s|$)", " "); //Remove S A 
            result = Regex.Replace(result, @"\s+", " "); //Trim consecutive white spaces
            result = Regex.Replace(result, @" SA$", " "); //Remove SA in the end (to avoid removing SA DA ... etc)
            result = result.Trim();

            return result;
        }

    }
}
