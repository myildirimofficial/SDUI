using Orivy.Controls;

namespace Orivy.Validations;

public class RequiredFieldValidationRule : ValidationRule
{
    public override bool Validate(ElementBase element, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(element.Text))
        {
            errorMessage = ErrorMessage ?? "Bu alan boş bırakılamaz.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}