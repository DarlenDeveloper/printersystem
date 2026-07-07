using ChequeMate.Core.Services;

namespace ChequeMate.Infrastructure.Services;

public class AmountToWordsService : IAmountToWordsService
{
    private static readonly string[] Ones =
    {
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
        "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen",
        "Sixteen", "Seventeen", "Eighteen", "Nineteen"
    };

    private static readonly string[] Tens =
    {
        "", "", "Twenty", "Thirty", "Forty", "Fifty",
        "Sixty", "Seventy", "Eighty", "Ninety"
    };

    public string Convert(decimal amount, string currency = "")
    {
        if (amount < 0)
            return "Negative " + Convert(-amount, currency);

        if (amount == 0)
            return "Zero Only";

        var wholePart = (long)Math.Floor(amount);
        var fractionalPart = (int)Math.Round((amount - wholePart) * 100);

        var result = string.Empty;

        if (wholePart > 0)
        {
            result = ConvertWholeNumber(wholePart);
        }

        if (fractionalPart > 0)
        {
            if (wholePart > 0)
                result += " and ";
            result += ConvertWholeNumber(fractionalPart) + " Cents";
        }

        result += " Only";

        if (!string.IsNullOrWhiteSpace(currency))
            result = currency + " " + result;

        return result.Trim();
    }

    private static string ConvertWholeNumber(long number)
    {
        if (number == 0) return string.Empty;
        if (number < 0) return "Negative " + ConvertWholeNumber(-number);

        var words = string.Empty;

        if (number / 10_000_000 > 0)
        {
            words += ConvertWholeNumber(number / 10_000_000) + " Crore ";
            number %= 10_000_000;
        }

        if (number / 100_000 > 0)
        {
            words += ConvertWholeNumber(number / 100_000) + " Lakh ";
            number %= 100_000;
        }

        if (number / 1_000 > 0)
        {
            words += ConvertWholeNumber(number / 1_000) + " Thousand ";
            number %= 1_000;
        }

        if (number / 100 > 0)
        {
            words += ConvertWholeNumber(number / 100) + " Hundred ";
            number %= 100;
        }

        if (number > 0)
        {
            if (!string.IsNullOrEmpty(words))
                words += "and ";

            if (number < 20)
            {
                words += Ones[number];
            }
            else
            {
                words += Tens[number / 10];
                if (number % 10 > 0)
                    words += " " + Ones[number % 10];
            }
        }

        return words.Trim();
    }
}
