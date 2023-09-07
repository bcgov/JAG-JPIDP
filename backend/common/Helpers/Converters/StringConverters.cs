namespace Common.Helpers.Converters;
using System;
using System.Text.RegularExpressions;

public class StringConverters
{
    public static string ConvertPhoneNumber(string phoneNumber) => String.Format("{0:(###) ###-####}", GetNumbersOnly(phoneNumber));

    public static string GetNumbersOnly(string str) => Regex.Replace(str, "[^0-9]+", "", RegexOptions.Compiled);
}
