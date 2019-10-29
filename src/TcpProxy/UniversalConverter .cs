using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace TcpProxy
{

    public class UniversalConverter : IValueConverter
    {
        private object TryGetValueFromResource(string strValueKey)
        {
            if (string.IsNullOrWhiteSpace(strValueKey))
            {
                return null;
            }
            strValueKey = strValueKey.Trim();
            object value = null;
            if (strValueKey.StartsWith("@"))
            {
                if (strValueKey.Length <= 1)
                {
                    return value;
                }
                strValueKey = strValueKey.TrimStart('@');
                if (System.Windows.Application.Current.Resources.Contains(strValueKey))
                {
                    value = System.Windows.Application.Current.Resources[strValueKey];
                }
            }
            else
            {
                value = strValueKey;
            }
            return value;
        }
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string strParam = parameter as string;
            if (string.IsNullOrEmpty(strParam))
            {
                return value;
            }
            object outDefaultVal = null;
            try
            {
                List<string> lstExpression = new List<string>();
                MatchCollection matchList = Regex.Matches(strParam, @"\[(.*?)\]");
                if (matchList != null && matchList.Count > 0)
                {
                    foreach (Match m in matchList)
                    {
                        string strExpression = m.Groups[1].Value;
                        lstExpression.Add(strExpression);
                    }
                    
                    foreach (string exp in lstExpression)
                    {
                        string[] exArray = exp.Split(':');
                        if (outDefaultVal == null)
                        {
                            outDefaultVal = TryGetValueFromResource(exArray[exArray.Length == 1 ? 0 : 1]);
                            if (outDefaultVal == null)
                            {
                                continue;
                            }
                            if (value == null)
                            {
                                return outDefaultVal;
                            }
                        }

                        if (exArray.Length >= 2)
                        {
                            string strInVal = value.ToString().ToLower();
                            if (ExpressionHandler.IsMatch(strInVal, exArray[0]))
                            {
                                object outValue = TryGetValueFromResource(exArray[1]);
                                if(outValue == null)
                                {
                                    continue;
                                }
                                return outValue;
                            }
                        }
                    }
                    return outDefaultVal;
                }
                else
                {
                    return value;
                }
            }
            catch
            {
                if(outDefaultVal != null)
                {
                    return outDefaultVal;
                }
                return value;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class ExpressionHandler
    {
        private const string BRACKET = "\\(([^\\(\\)]*?)\\)";

        public static bool IsMatch(string inValue, string filterExpression)
        {
            filterExpression = filterExpression.ToLower().Trim();
            filterExpression = filterExpression.Replace(" ", "");
            filterExpression = filterExpression.Replace("\r", "");
            filterExpression = filterExpression.Replace("\n", "");
            filterExpression = HandleBracket(filterExpression, inValue);
            return DoMatch(filterExpression, inValue);
        }

        private static string HandleBracket(string filterExpression, string inValue)
        {
            while (Regex.IsMatch(filterExpression, BRACKET))
            {
                Match m = Regex.Match(filterExpression, BRACKET);
                if (m.Success)
                {
                    string toReplace = m.Groups[0].Value;
                    string innerExpression = m.Groups[1].Value;
                    string replacStr = DoMatch(innerExpression, inValue) ? "[TRUE]" : "[FALSE]";
                    filterExpression = filterExpression.Replace(toReplace, replacStr);
                }
            }
            return filterExpression;
        }


        private static bool DoMatch(string filterExpression, string inValue)
        {
            if (string.IsNullOrEmpty(filterExpression))
            {
                return true;
            }
            else if (filterExpression == "![TRUE]" || filterExpression == "！[TRUE]")
            {
                return false;
            }
            else if (filterExpression == "[TRUE]")
            {
                return true;
            }
            else if (filterExpression == "![FALSE]" || filterExpression == "！[FALSE]")
            {
                return true;
            }
            else if (filterExpression == "[FALSE]")
            {
                return false;
            }
            else if (filterExpression.Contains('|'))
            {
                string[] orList = filterExpression.Split('|');
                foreach (string orExpression in orList)
                {
                    if (DoMatch(orExpression, inValue))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (filterExpression.Contains('&'))
            {
                string[] andList = filterExpression.Split('&');
                foreach (string andExpression in andList)
                {
                    if (!DoMatch(andExpression, inValue))
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                bool bRelative = false;
                if (filterExpression.StartsWith("!") || filterExpression.StartsWith("！"))
                {
                    bRelative = true;
                    filterExpression = filterExpression.Substring(1);
                    if (filterExpression == "")
                    {
                        return true;
                    }
                }

                if (bRelative)
                {
                    if (!string.IsNullOrEmpty(inValue) && inValue.Contains(filterExpression))
                    {
                        return false;
                    }

                    return true;
                }
                else
                {
                    if (!string.IsNullOrEmpty(inValue))
                    {
                        if (inValue.Contains(filterExpression))
                        {
                            return true;
                        }
                    }
                    return false;
                }

            }
        }
    }
}
