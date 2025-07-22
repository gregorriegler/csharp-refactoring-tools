# ExtractMethod Refactoring - Missing Cases and Improvements

## TDD Phase: 🔴

## Scenarios

### Extraction - REFINED

Examples (ordered by simplicity):
- [x] Extract single variable: `var result = x;` → extract `x`
- [x] Extract simple addition: `var result = a + b;` → extract `a + b`
- [x] Extract with precedence: `var result = a + b * c;` → extract `a + b * c`
- [x] Extract method call: `var result = Math.Max(a, b);` → extract `Math.Max(a, b)`
- [x] Extract nested expressions: `var result = Math.Max(a + 1, b * 2);` → extract `Math.Max(a + 1, b * 2)`
- [x] Extract single statement: `Console.WriteLine("Hello");` → extract into void method
- [x] Extract two statements: `var x = 10; Console.WriteLine(x);` → extract both into void method
- [x] Extract three statements with local variables: `var x = 10; var y = 20; Console.WriteLine(x + y);` → extract all into void method
- [x] Extract code with loop that calculates and returns a single value
- [x] Extract LINQ method chain spanning multiple lines
- [x] Extract if-else conditional logic into separate method

### Loop Body Extraction - REFINED
- [x] Extract the body of a simple loop into a method.
```csharp
foreach (var item in items)
{
    item.Process();
    item.Validate();
    results.Add(item.GetResult());
}
// Extract: item processing logic
```

### Exception Handling Extraction - REFINED
- [x] Extract code that includes try-catch blocks.
```csharp
try
{
    var data = LoadData(filename);
    var processed = ProcessData(data);
    SaveData(processed, outputFile);
}
catch (FileNotFoundException ex)
{
    LogError($"File not found: {ex.Message}");
    return false;
}
// Extract: file processing with error handling
```

### Async Method Extraction - DRAFT
Extract code containing await expressions.
```csharp
var client = new HttpClient();
var response = await client.GetAsync(url);
var content = await response.Content.ReadAsStringAsync();
return JsonSerializer.Deserialize<T>(content);
// Extract: HTTP request and deserialization logic
```

### Multiple Return Values via Tuple - REFINED
- [x] Extract code that needs to return multiple values using tuples.
```csharp
var isValid = ValidateInput(input);
var errorMessage = GetValidationError(input);
var processedValue = ProcessInput(input);
return (isValid, errorMessage, processedValue);
// Extract: validation and processing logic returning (bool, string, object)
```

### Invalid Selection Boundaries - DRAFT
User selects partial statements or crosses method boundaries.
```csharp
var x = SomeMethod(
    parameter1,
    // User selects from here
    parameter2);
var y = x + 1;
// to here - invalid selection
```

### Variable Scope Conflicts - DRAFT
Extracted code would create variable naming conflicts.
```csharp
var name = "outer";
{
    var name = "inner"; // conflict if extracted
    Console.WriteLine(name);
}
```

### Unreachable Code After Return - DRAFT
Selection includes code after a return statement.
```csharp
if (condition)
    return result;
Console.WriteLine("This might be unreachable");
// Extract: both statements together
```

### Missing Variable Dependencies - DRAFT
Selected code uses variables not available in extraction scope.
```csharp
var localVar = GetValue();
// ... many lines later in different scope
Console.WriteLine(localVar); // localVar not accessible
```

## Coverage Analysis - Lines Needing Tests

### TypeInferrer Error Handling - REFINED
- [x] Test type inference when semantic model returns error types.
```csharp
// Test case: TypeInferrer.GetTypeDisplayString with null/error type
var inferrer = new TypeInferrer();
var result = inferrer.InferType(errorExpression, semanticModel);
// Should return "object" for error types
```

### CodeSelection Validation - DRAFT
Test CodeSelection.IsInRange with edge cases.
```csharp
// Test case: Selection beyond file boundaries
var selection = CodeSelection.Parse("100:1-100:10");
var result = selection.IsInRange(shortFileLines);
// Should return false for out-of-range selections
```

### ExtractMethod Error Cases - DRAFT
Test ExtractMethod when no valid extraction target found.
```csharp
// Test case: ExtractMethod with invalid selection
var extractMethod = ExtractMethod.Create(["1:1-1:1"]);
var result = await extractMethod.PerformAsync(emptyDocument);
// Should handle gracefully when no extraction possible
```

### InlineMethod Edge Cases - DRAFT
Test InlineMethod with complex method signatures.
```csharp
// Test case: InlineMethod with generic method parameters
var inlineMethod = InlineMethod.Create(["methodName"]);
var result = await inlineMethod.PerformAsync(genericMethodDocument);
// Should handle generic method inlining
```

### MoveMemberUp Validation - DRAFT
Test MoveMemberUp error handling paths.
```csharp
// Test case: MoveMemberUp with missing class
var moveMember = new MoveMemberUp("NonExistentClass", "SomeMethod");
var result = await moveMember.PerformAsync(document);
// Should handle gracefully when class not found

// Test case: MoveMemberUp with no base class
var moveMember = new MoveMemberUp("SealedClass", "SomeMethod");
var result = await moveMember.PerformAsync(sealedClassDocument);
// Should handle gracefully when no base class available

// Test case: MoveMemberUp with missing method
var moveMember = new MoveMemberUp("DerivedClass", "NonExistentMethod");
var result = await moveMember.PerformAsync(document);
// Should handle gracefully when method not found
```

### RenameSymbol Error Handling - DRAFT
Test RenameSymbol with unsupported symbol types.
```csharp
// Test case: RenameSymbol with unsupported token
var renameSymbol = new RenameSymbol(new Cursor(1, 1), "newName");
var result = await renameSymbol.PerformAsync(documentWithKeyword);
// Should handle gracefully when cursor is on keyword or unsupported symbol
```

### ExtractCollaboratorInterface Null Handling - DRAFT
Test ExtractCollaboratorInterface with null inputs.
```csharp
// Test case: ExtractCollaboratorInterface with null document
var extractor = new ExtractCollaboratorInterface(validSelection);
var result = await extractor.PerformAsync(null);
// Should throw ArgumentNullException

// Test case: ExtractCollaboratorInterface with null syntax root
var extractor = new ExtractCollaboratorInterface(validSelection);
var result = await extractor.PerformAsync(documentWithNullRoot);
// Should return original document when syntax root is null
```

### TypeInferrer Fallback Paths - DRAFT
Test TypeInferrer when semantic model fails.
```csharp
// Test case: TypeInferrer with error type in await expression
var inferrer = new TypeInferrer();
var result = inferrer.InferType(awaitExpressionWithErrorType, semanticModel);
// Should return correct fallback type

// Test case: TypeInferrer with error type in regular expression
var inferrer = new TypeInferrer();
var result = inferrer.InferType(regularExpressionWithErrorType, semanticModel);
// Should fall back to pattern-based inference
```
