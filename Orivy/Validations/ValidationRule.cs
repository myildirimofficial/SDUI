using Orivy.Controls;

namespace Orivy.Validations;

public abstract class ValidationRule
{
    public string ErrorMessage { get; set; }
    public abstract bool Validate(ElementBase element, out string errorMessage);
}