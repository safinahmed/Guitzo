using System.Globalization;
using System.Xml.Linq;

namespace DataETLTest
{
    /// <summary>
    /// Class that will contain all Constants
    /// Needs to be in a higher hierarchy level (no inside ETL only)
    /// </summary>
    public class Constants
    {
        public const int StatusOk = 0;
        public const int StatusNok = -1;

        public const int Debt = 1;
        public const int Credit = 2;

        public const int TPA = 1;
        public const int PGT = 2;
        public const int DEB = 3;
        public const int TRF = 4;
        public const int LEV = 5;
        public const int SDD = 6;
        public const int CRD = 7;
        public const int STN = 8;


        public static CultureInfo CulturePT = CultureInfo.CreateSpecificCulture("pt-PT");
        public static CultureInfo CultureUS = CultureInfo.CreateSpecificCulture("en-US");
        public static TextInfo TextInfo = CulturePT.TextInfo;

        public static XNamespace ExcelNS = "urn:schemas-microsoft-com:office:spreadsheet";
    }
}
