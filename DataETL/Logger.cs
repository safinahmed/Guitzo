using System;
using OutSystems.HubEdition.RuntimePlatform;

namespace DataETLTest
{
    public class Logger
    {
        public static void log(string message, string module)
        {
            try
            {
                var h = new HeContext();
                GenericExtendedActions.Audit(h,message,module);
            }
            catch (Exception)
            {
            }
        }
    }
}
