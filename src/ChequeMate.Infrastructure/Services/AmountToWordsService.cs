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

    private static readonly (long Value, string Name)[] Scales =
    {
        (1_000_000_000_000_000_000, "Quintillion"),
        (1_000_000_000_000_000, "Quadrillion"),
        (1_000_000_000_000, "Trillion"),
        (1_000_000_000, "Billion"),
        (1_000_000, "Million"),
        (1_000, "Thousand")
    };

    public string Convert(decimal amount, string currency = "", string fractionalUnit = "Cents")
    {
        var isNegative = amount < 0;
        var rounded = decimal.Round(decimal.Abs(amount), 2, MidpointRounding.AwayFromZero);

        if (rounded > long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount is too large to convert to words.");

        var wholePart = (long)decimal.Truncate(rounded);
        var fractionalPart = (int)((rounded - wholePart) * 100);
        var wholeWords = ConvertWholeNumber(wholePart);

        var result = string.IsNullOrWhiteSpace(currency)
            ? wholeWords
            : $"{currency.Trim()} {wholeWords}";

        if (fractionalPart > 0)
        {
            var unit = fractionalPart == 1
                ? Singularize(fractionalUnit)
                : fractionalUnit.Trim();
            result += $" and {ConvertWholeNumber(fractionalPart)} {unit}";
        }

        result += " Only";
        return isNegative ? $"Negative {result}" : result;
    }

    private static string ConvertWholeNumber(long number)
    {
        if (number == 0)
            return "Zero";

        foreach (var scale in Scales)
        {
            if (number < scale.Value)
                continue;

            var leading = ConvertWholeNumber(number / scale.Value);
            var remainder = number % scale.Value;
            if (remainder == 0)
                return $"{leading} {scale.Name}";

            var separator = remainder < 100 ? " and " : " ";
            return $"{leading} {scale.Name}{separator}{ConvertWholeNumber(remainder)}";
        }

        return ConvertBelowOneThousand((int)number);
    }

    private static string ConvertBelowOneThousand(int number)
    {
        if (number < 20)
            return Ones[number];

        if (number < 100)
        {
            var remainder = number % 10;
            return remainder == 0
                ? Tens[number / 10]
                : $"{Tens[number / 10]} {Ones[remainder]}";
        }

        var hundreds = $"{Ones[number / 100]} Hundred";
        var rest = number % 100;
        return rest == 0
            ? hundreds
            : $"{hundreds} and {ConvertBelowOneThousand(rest)}";
    }

    private static string Singularize(string value)
    {
        var trimmed = value.Trim();
        return trimmed.EndsWith("s", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^1]
            : trimmed;
    }
}
