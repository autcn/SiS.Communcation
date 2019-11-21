using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiS.Communication
{
    /// <summary>
    /// Provides simple static methods to extend the Exception class.
    /// </summary>
    public static class ExceptionEx
    {
        /// <summary>
        /// Get combined message of all exceptions, include inner exception.
        /// </summary>
        /// <returns>The combined message of all exceptions.</returns>
        public static string MessageAll(this Exception ex)
        {
            string info = ex.Message;
            Exception curEx = ex;
            while (curEx.InnerException != null)
            {
                string innerMsg = curEx.InnerException.Message == null ? "" : curEx.InnerException.Message;
                if (info != "")
                {
                    info += "\r\n" + innerMsg;
                }
                curEx = curEx.InnerException;
            }
            return info;
        }
    }
}
