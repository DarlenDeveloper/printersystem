namespace ChequeMate.Core.Services;

public interface IAmountToWordsService
{
    /// <summary>
    /// Converts a numeric amount to its word representation.
    /// E.g., 1500.50 → "One Thousand Five Hundred and Fifty Cents Only"
    /// </summary>
    string Convert(decimal amount, string currency = "");
}
