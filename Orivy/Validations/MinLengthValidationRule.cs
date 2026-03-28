using Orivy.Controls;

namespace Orivy.Validations;

public class MinLengthValidationRule : ValidationRule
{
    public int MinLength { get; set; }

    public override bool Validate(ElementBase element, out string errorMessage)
    {
        if (element.Text.Length < MinLength)
        {
            errorMessage = ErrorMessage ?? $"Bu alan en az {MinLength} karakter olmalıdır.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}