using Orivy.Controls;

namespace Orivy.Validations;

public class MaxLengthValidationRule : ValidationRule
{
    public int MaxLength { get; set; }

    public override bool Validate(ElementBase element, out string errorMessage)
    {
        if (element.Text.Length > MaxLength)
        {
            errorMessage = ErrorMessage ?? $"Bu alan en fazla {MaxLength} karakter olmalıdır.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}