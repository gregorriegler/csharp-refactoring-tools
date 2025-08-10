## Original

```csharp

public class ValidationProcessor
{
    public (bool, string, object) ProcessInput(string input)
    {
        var isValid = ValidateInput(input);
        var errorMessage = GetValidationError(input);
        var processedValue = ProcessInput(input);
        return (isValid, errorMessage, processedValue);
    }

    private bool ValidateInput(string input) => !string.IsNullOrEmpty(input);
    private string GetValidationError(string input) => string.IsNullOrEmpty(input) ? "Invalid input" : "";
    private object ProcessInput(string input) => input?.ToUpper();
}
```

## Selected Span

```csharp
        var isValid = ValidateInput(input);
        var errorMessage = GetValidationError(input);
        var processedValue = ProcessInput(input);
    
```

---

## Refactored

```csharp
public class ValidationProcessor
{
    public (bool, string, object) ProcessInput(string input)
    {
        (isValid, errorMessage, processedValue) = ValidateAndProcess(input);
        return (isValid, errorMessage, processedValue);
    }

    private (bool, string, object) ValidateAndProcess(string input)
    {
        var isValid = ValidateInput(input);
        var errorMessage = GetValidationError(input);
        var processedValue = ProcessInput(input);
        return (isValid, errorMessage, processedValue);
    }

    private bool ValidateInput(string input) => !string.IsNullOrEmpty(input);
    private string GetValidationError(string input) => string.IsNullOrEmpty(input) ? "Invalid input" : "";
    private object ProcessInput(string input) => input?.ToUpper();
}
```