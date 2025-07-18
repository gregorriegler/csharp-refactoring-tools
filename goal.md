# ExtractMethod Refactoring - Missing Cases and Improvements

## TDD Phase: 🧹

### 1. Expression-Only Extraction
**Problem**: Current implementation focuses on `StatementSyntax` but fails when extracting pure expressions.
**Example**: 
```csharp
var result = someComplexCalculation + anotherComplexCalculation;
//           ^-- extracting just this expression would fail
```
**Goal**: Support extracting expressions that aren't complete statements.

### 2. Multiple Return Values
**Problem**: Only handles single return values (lines 71-73, 102-105).
**Missing Support**:
- Tuples: `(int, string)`
- Out parameters: `out int value`
- Ref parameters: `ref int value`
**Goal**: Extract code that needs to return multiple values via tuples or ref/out parameters.

### 3. Async/Await Context
**Problem**: No handling of `async`/`await` - extracting code containing `await` expressions breaks compilation.
**Goal**: Properly handle async method extraction and preserve async context.

### 4. Exception Handling Boundaries
**Problem**: Doesn't consider try-catch-finally blocks.
**Goal**: Safely extract code that crosses exception handling boundaries without breaking exception flow.

### 5. Static vs Instance Context
**Problem**: Always creates `private` instance methods.
**Missing Support**:
- Static methods when extracting from static context
- Public/protected accessibility when needed
- Generic type parameters
**Goal**: Create methods with appropriate accessibility and static/instance modifiers.

### 6. Resource Management (Using Statements)
**Problem**: Extracting code with `using` statements or `IDisposable` patterns breaks resource disposal.
**Goal**: Preserve resource management patterns when extracting code.

### 7. Control Flow Statements
**Problem**: No handling of complex control flow.
**Missing Support**:
- `goto` statements
- `break`/`continue` in loops
- `yield return`/`yield break`
**Goal**: Handle control flow statements that affect method boundaries.

### 8. Local Functions and Lambdas
**Problem**: May not correctly handle modern C# constructs.
**Missing Support**:
- Local function declarations within extracted code
- Lambda expressions with captured variables
- Anonymous methods
**Goal**: Properly extract code containing local functions and lambda expressions.

### 9. Partial Statement Selection Issues
**Problem**: Logic for partial selections (lines 36-52) uses `OverlapsWith` which could select partial statements incorrectly.
**Goal**: Improve selection logic to handle partial statement selections accurately.

### 10. Complex Data Flow Analysis
**Problem**: Data flow analysis (lines 61-73) doesn't handle advanced scenarios.
**Missing Support**:
- Ref/out parameters properly
- Complex closure scenarios
- Field vs local variable distinction
**Goal**: Enhance data flow analysis for complex variable usage patterns.

## Scenarios

### Simple Expression Extraction - REFINED
Extract a single arithmetic expression from an assignment statement.

Examples (ordered by simplicity):
- [x] Extract single variable: `var result = x;` → extract `x`
- [x] Extract simple addition: `var result = a + b;` → extract `a + b`
- [x] Extract with precedence: `var result = a + b * c;` → extract `a + b * c`
- [x] Extract method call: `var result = Math.Max(a, b);` → extract `Math.Max(a, b)`
- [x] Extract nested expressions: `var result = Math.Max(a + 1, b * 2);` → extract `Math.Max(a + 1, b * 2)`

### Simple Statement Block Extraction - REFINED
Extract a sequence of simple statements that don't return values.
```csharp
var x = 10;
var y = 20;
Console.WriteLine(x + y);
// Extract: all three statements into a void method
```

Examples (ordered by simplicity):
- [x] Extract single statement: `Console.WriteLine("Hello");` → extract into void method
- [x] Extract two statements: `var x = 10; Console.WriteLine(x);` → extract both into void method
- [x] Extract three statements with local variables: `var x = 10; var y = 20; Console.WriteLine(x + y);` → extract all into void method

### Single Return Value Extraction - REFINED
Extract code that calculates and returns a single value.
```csharp
var total = 0;
for (int i = 0; i < items.Length; i++)
{
    total += items[i];
}
return total;
// Extract: calculation logic into a method returning int
```

- [x] Extract code with loop that calculates and returns a single value

### Local Variable Usage Extraction - REFINED
Extract code that uses local variables as parameters.

This scenario is already fully implemented by the existing ExtractMethod functionality. The current implementation correctly identifies local variables used in selected code and passes them as parameters to the extracted method.

### Method Call Chain Extraction - REFINED
Extract a chain of method calls into a separate method.
```csharp
var result = data
    .Where(x => x.IsActive)
    .Select(x => x.Name)
    .OrderBy(x => x)
    .ToList();
// Extract: the entire LINQ chain
```

- [x] Extract LINQ method chain spanning multiple lines

### Conditional Logic Extraction - REFINED
Extract simple if-else logic into a method.
```csharp
if (user.Age >= 18)
{
    user.CanVote = true;
    user.Status = "Adult";
}
else
{
    user.CanVote = false;
    user.Status = "Minor";
}
// Extract: age-based status setting logic
```

- [x] Extract if-else conditional logic into separate method

### Loop Body Extraction - DRAFT
Extract the body of a simple loop into a method.
```csharp
foreach (var item in items)
{
    item.Process();
    item.Validate();
    results.Add(item.GetResult());
}
// Extract: item processing logic
```

### Exception Handling Extraction - DRAFT
Extract code that includes try-catch blocks.
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

### Multiple Return Values via Tuple - DRAFT
Extract code that needs to return multiple values using tuples.
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
