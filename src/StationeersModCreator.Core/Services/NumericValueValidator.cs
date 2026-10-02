using System.Globalization;

namespace StationeersModCreator.Core.Services;
public sealed class NumericValueValidator
{
    public decimal Parse(string name, string value)
    {
        if (!decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || number < 0)
            throw new InvalidDataException(name + " must be a finite, nonnegative number using a decimal point.");
        return number;
    }

    public void ValidateQuantity(string name, string value)
    {
        var number = Parse(name, value);
        if (name == "MaxQuantity" && (number < 1 || decimal.Truncate(number) != number))
            throw new InvalidDataException("MaxQuantity must be a positive whole number.");
    }
}
