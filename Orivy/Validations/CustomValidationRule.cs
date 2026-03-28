using System;
using Orivy.Controls;

namespace Orivy.Validations;

public class CustomValidationRule : ValidationRule
{
    private readonly Func<ElementBase, (bool isValid, string errorMessage)> _validationFunc;

    public CustomValidationRule(Func<ElementBase, (bool isValid, string errorMessage)> validationFunc)
    {
        _validationFunc = validationFunc;
    }

    public override bool Validate(ElementBase element, out string errorMessage)
    {
        var result = _validationFunc(element);
        errorMessage = result.errorMessage;
        return result.isValid;
    }
}