# ExtractMethod Refactoring - Missing Cases and Improvements

## TDD Phase: 🧹

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

### Async Method Extraction - REFINED
- [x] Extract code containing await expressions.
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

### Invalid Selection Boundaries - REFINED
- [x] User selects partial statements or crosses method boundaries.
```csharp
var x = SomeMethod(
    parameter1,
    // User selects from here
    parameter2);
var y = x + 1;
// to here - invalid selection
```

### Variable Scope Conflicts - REFINED
- [x] Extracted code would create variable naming conflicts.
```csharp
var name = "outer";
{
    var name = "inner"; // conflict if extracted
    Console.WriteLine(name);
}
```

### Unreachable Code After Return - REFINED
- [x] Selection includes code after a return statement.
```csharp
if (condition)
    return result;
Console.WriteLine("This might be unreachable");
// Extract: both statements together
```

### Missing Variable Dependencies - REFINED
- [x] Selected code uses variables not available in extraction scope.
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

### CodeSelection Validation - REFINED
- [x] Test CodeSelection.IsInRange with edge cases.
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

### CodeSelection Parse Error Handling - DRAFT
Test CodeSelection.Parse with invalid cursor format.
```csharp
// Test case: Cursor.Parse with invalid format
var cursor = Cursor.Parse("invalid:format:extra");
// Should throw InvalidOperationException

// Test case: Cursor.Parse with non-numeric values
var cursor = Cursor.Parse("abc:def");
// Should throw InvalidOperationException
```

### CsProject Infrastructure - DRAFT
Test CsProject file handling edge cases.
```csharp
// Test case: CsProject with missing file
var project = new CsProject("valid.csproj", "missing.cs");
await project.OpenAndApplyRefactoring(refactoring);
// Should handle gracefully when file not found

// Test case: CsProject MSBuild registration
var project = new CsProject("valid.csproj", "existing.cs");
await project.OpenAndApplyRefactoring(refactoring);
// Should register MSBuild defaults and apply changes
```

### InlineMethod Error Paths - DRAFT
Test InlineMethod with missing method declarations.
```csharp
// Test case: InlineMethod with method declaration not found
var inlineMethod = InlineMethod.Create(["1:1"]);
var result = await inlineMethod.PerformAsync(documentWithMissingMethod);
// Should return original document when method declaration not found

// Test case: InlineMethod with null method body
var inlineMethod = InlineMethod.Create(["1:1"]);
var result = await inlineMethod.PerformAsync(documentWithAbstractMethod);
// Should return original document when method has no body
```

### StatementExtractionTarget Edge Cases - DRAFT
Test StatementExtractionTarget with complex scenarios.
```csharp
// Test case: StatementExtractionTarget with nested blocks
var target = new StatementExtractionTarget(nestedStatements, block, semanticModel);
var result = target.CreateReplacementNode("ExtractedMethod");
// Should handle nested statement extraction

// Test case: StatementExtractionTarget with variable scope analysis
var target = new StatementExtractionTarget(statementsWithVariables, block, semanticModel);
var parameters = target.GetParameters();
// Should identify required parameters from variable usage
```

### ExpressionExtractionTarget Type Inference - DRAFT
Test ExpressionExtractionTarget with complex expressions.
```csharp
// Test case: ExpressionExtractionTarget with async expressions
var target = new ExpressionExtractionTarget(awaitExpression, semanticModel);
var returnType = target.DetermineReturnType();
// Should handle async return type inference

// Test case: ExpressionExtractionTarget with generic expressions
var target = new ExpressionExtractionTarget(genericExpression, semanticModel);
var returnType = target.DetermineReturnType();
// Should handle generic type inference
```

### Program.cs Infrastructure Coverage - DRAFT
Test main program entry points for both analysis and refactoring tools.
```csharp
// Test case: RoslynAnalysis with --list-tools argument
var args = new[] { "--list-tools" };
// Should output JSON list of available analyses

// Test case: RoslynAnalysis with missing project path
var args = new[] { "analysis-name" };
// Should output usage error message

// Test case: RoslynRefactoring with --list-tools argument
var args = new[] { "--list-tools" };
// Should output JSON list of available refactorings
```

### CodeSelection Error Handling - DRAFT
Test CodeSelection parsing with invalid inputs.
```csharp
// Test case: CodeSelection.Parse with wrong format
var selection = CodeSelection.Parse("1:1:1-2:2");
// Should throw InvalidOperationException

// Test case: Cursor.Parse with non-numeric parts
var cursor = Cursor.Parse("abc:def");
// Should throw InvalidOperationException

// Test case: CodeSelection.Create with invalid line numbers
var selection = CodeSelection.Create(new Cursor(0, 1), new Cursor(1, 1));
// Should throw InvalidOperationException for line <= 0
```

### CsProject File Operations - DRAFT
Test CsProject with missing files and MSBuild operations.
```csharp
// Test case: CsProject with file not found in project
var project = new CsProject("valid.csproj", "missing.cs");
await project.OpenAndApplyRefactoring(mockRefactoring);
// Should handle gracefully and log error message

// Test case: CsProject MSBuild workspace operations
var project = new CsProject("valid.csproj", "existing.cs");
await project.OpenAndApplyRefactoring(mockRefactoring);
// Should successfully apply refactoring and workspace changes
```

### TypeInferrer Error Type Handling - DRAFT
Test TypeInferrer with error types and fallback scenarios.
```csharp
// Test case: TypeInferrer with await expression error type
var inferrer = new TypeInferrer();
var result = inferrer.InferType(awaitExpressionWithErrorType, semanticModel);
// Should return "object" when type inference fails

// Test case: TypeInferrer with error type expression
var inferrer = new TypeInferrer();
var result = inferrer.InferType(errorTypeExpression, semanticModel);
// Should fall back to pattern-based type inference
```

### ExtractCollaboratorInterface Edge Cases - DRAFT
Test ExtractCollaboratorInterface with null document root.
```csharp
// Test case: ExtractCollaboratorInterface with null syntax root
var extractor = new ExtractCollaboratorInterface(validSelection);
var result = await extractor.PerformAsync(documentWithNullRoot);
// Should return original document when syntax root is null
```

### ExtractionTarget Complex Scenarios - DRAFT
Test ExtractionTarget with edge cases and error conditions.
```csharp
// Test case: ExtractionTarget with no valid statements or expressions
var target = ExtractionTarget.CreateFromSelection(emptyNode, emptySpan, block, semanticModel);
// Should throw InvalidOperationException when nothing to extract

// Test case: ExtractionTarget with complex nested expressions
var target = ExtractionTarget.CreateFromSelection(nestedExpressionNode, span, block, semanticModel);
// Should correctly identify and extract nested expressions
```

### ExtractMethod Error Conditions - DRAFT
Test ExtractMethod with invalid selections and null conditions.
```csharp
// Test case: ExtractMethod with null syntax root
var extractMethod = ExtractMethod.Create(["1:1-2:2", "TestMethod"]);
var result = await extractMethod.PerformAsync(documentWithNullRoot);
// Should throw InvalidOperationException

// Test case: ExtractMethod with no containing block
var extractMethod = ExtractMethod.Create(["1:1-2:2", "TestMethod"]);
var result = await extractMethod.PerformAsync(documentWithoutBlock);
// Should throw InvalidOperationException

// Test case: ExtractMethod with null semantic model
var extractMethod = ExtractMethod.Create(["1:1-2:2", "TestMethod"]);
var result = await extractMethod.PerformAsync(documentWithNullSemanticModel);
// Should throw InvalidOperationException
```

### InlineMethod Complex Scenarios - DRAFT
Test InlineMethod with missing methods and null bodies.
```csharp
// Test case: InlineMethod with method declaration not found
var inlineMethod = InlineMethod.Create(["1:1"]);
var result = await inlineMethod.PerformAsync(documentWithMissingMethod);
// Should return original document when method not found

// Test case: InlineMethod with null method body
var inlineMethod = InlineMethod.Create(["1:1"]);
var result = await inlineMethod.PerformAsync(documentWithAbstractMethod);
// Should return original document when method has no body
```

### MoveMemberUp Error Handling - DRAFT
Test MoveMemberUp with missing classes and methods.
```csharp
// Test case: MoveMemberUp with class not found
var moveMember = new MoveMemberUp("NonExistentClass", "SomeMethod");
var result = await moveMember.PerformAsync(document);
// Should return original document and log error

// Test case: MoveMemberUp with no base class
var moveMember = new MoveMemberUp("SealedClass", "SomeMethod");
var result = await moveMember.PerformAsync(sealedClassDocument);
// Should return original document and log error

// Test case: MoveMemberUp with method not found
var moveMember = new MoveMemberUp("DerivedClass", "NonExistentMethod");
var result = await moveMember.PerformAsync(document);
// Should return original document and log error
```

### RenameSymbol Error Paths - DRAFT
Test RenameSymbol with unsupported symbols and error conditions.
```csharp
// Test case: RenameSymbol with non-identifier token
var renameSymbol = new RenameSymbol(new Cursor(1, 1), "newName");
var result = await renameSymbol.PerformAsync(documentWithKeyword);
// Should return original document when token is not renameable

// Test case: RenameSymbol with unsupported symbol type
var renameSymbol = new RenameSymbol(new Cursor(1, 1), "newName");
var result = await renameSymbol.PerformAsync(documentWithUnsupportedSymbol);
// Should return original document and log error message
```

### StatementExtractionTarget Advanced Cases - DRAFT
Test StatementExtractionTarget with complex return scenarios.
```csharp
// Test case: StatementExtractionTarget with multiple return paths
var target = new StatementExtractionTarget(statementsWithMultipleReturns, block, semanticModel);
var returnType = target.DetermineReturnType();
// Should handle complex return type inference

// Test case: StatementExtractionTarget with async statements
var target = new StatementExtractionTarget(asyncStatements, block, semanticModel);
var methodDecl = target.CreateMethodDeclaration("ExtractedMethod");
// Should create async method declaration

// Test case: StatementExtractionTarget with tuple destructuring
var target = new StatementExtractionTarget(tupleStatements, block, semanticModel);
var replacement = target.CreateReplacementNode("ExtractedMethod");
// Should create tuple destructuring assignment
```

### ExpressionExtractionTarget Type Edge Cases - DRAFT
Test ExpressionExtractionTarget with complex type scenarios.
```csharp
// Test case: ExpressionExtractionTarget with error type
var target = new ExpressionExtractionTarget(errorTypeExpression, semanticModel);
var returnType = target.DetermineReturnType();
// Should fall back to pattern-based type inference

// Test case: ExpressionExtractionTarget with method symbol inference
var target = new ExpressionExtractionTarget(methodCallExpression, semanticModel);
var returnType = target.DetermineReturnType();
// Should infer return type from method symbol

// Test case: ExpressionExtractionTarget with Math.Max pattern
var target = new ExpressionExtractionTarget(mathMaxExpression, semanticModel);
var returnType = target.DetermineReturnType();
// Should return int type for Math.Max expressions
```

### ReturnBehavior Analysis - DRAFT
Test ReturnBehavior with complex control flow.
```csharp
// Test case: ReturnBehavior with switch statement all paths return
var behavior = new ReturnBehavior([switchStatementAllPathsReturn]);
var requiresReturn = behavior.RequiresReturnStatement;
// Should return true when all switch paths return or throw
```

### Additional Coverage - Dead Code Analysis Results

#### MoveMemberUp Remaining Error Paths - DRAFT
Test MoveMemberUp with null root and RemoveNode failure.
```csharp
// Test case: MoveMemberUp with null syntax root
var moveMember = new MoveMemberUp("TestClass", "TestMethod");
var result = await moveMember.PerformAsync(documentWithNullRoot);
// Should return original document when root is null

// Test case: MoveMemberUp with RemoveNode returning null
var moveMember = new MoveMemberUp("TestClass", "TestMethod");
var result = await moveMember.PerformAsync(documentWithUnremovableNode);
// Should return original root when RemoveNode fails
```

#### InlineMethod Remaining Error Paths - DRAFT
Test InlineMethod with method declaration not found.
```csharp
// Test case: InlineMethod with method declaration not found
var inlineMethod = InlineMethod.Create(["1:1"]);
var result = await inlineMethod.PerformAsync(documentWithMissingMethodDeclaration);
// Should return original document when method declaration not found

// Test case: InlineMethod with null method body after expression body check
var inlineMethod = InlineMethod.Create(["1:1"]);
var result = await inlineMethod.PerformAsync(documentWithMethodWithoutBody);
// Should return original document when method has no body
```

#### RenameSymbol Solution-Wide Error Path - DRAFT
Test RenameSymbol with null document root in solution-wide rename.
```csharp
// Test case: RenameSymbol solution-wide with null document root
var renameSymbol = new RenameSymbol(new Cursor(1, 1), "newName");
var result = await renameSymbol.PerformAsync(documentInSolutionWithNullRoot);
// Should continue processing other documents when one has null root
```

#### TypeInferrer Foreach Variable Resolution - DRAFT
Test TypeInferrer with missing method block and foreach statement.
```csharp
// Test case: TypeInferrer with null method block
var inferrer = new TypeInferrer();
var result = inferrer.ResolveActualTypeForForeachVariable(localSymbol, containingBlock, semanticModel);
// Should return "var" when method block not found

// Test case: TypeInferrer with missing foreach statement
var inferrer = new TypeInferrer();
var result = inferrer.ResolveActualTypeForForeachVariable(localSymbol, containingBlock, semanticModel);
// Should return "var" when foreach statement not found

// Test case: TypeInferrer with collection type without type arguments
var inferrer = new TypeInferrer();
var result = inferrer.ExtractElementTypeFromCollection(foreachWithoutTypeArgs, semanticModel);
// Should return "var" when collection has no type arguments
```

#### StatementExtractionTarget Complex Return Logic - DRAFT
Test StatementExtractionTarget with edge cases in return type determination.
```csharp
// Test case: StatementExtractionTarget with Task type wrapping
var target = new StatementExtractionTarget(awaitStatements, block, semanticModel);
var returnType = target.WrapInTaskType(voidType);
// Should wrap void in Task, non-void in Task<T>

// Test case: StatementExtractionTarget with existing Task type
var target = new StatementExtractionTarget(statements, block, semanticModel);
var returnType = target.WrapInTaskType(taskType);
// Should not double-wrap existing Task types

// Test case: StatementExtractionTarget with local declaration return
var target = new StatementExtractionTarget(localDeclStatements, block, semanticModel);
var methodBody = target.CreateMethodBody();
// Should add return statement for last local declaration

// Test case: StatementExtractionTarget with bool return requirement
var target = new StatementExtractionTarget(boolReturnStatements, block, semanticModel);
var methodBody = target.CreateMethodBody();
// Should add return true for bool methods without explicit return

// Test case: StatementExtractionTarget with tuple destructuring
var target = new StatementExtractionTarget(multiReturnStatements, block, semanticModel);
var replacement = target.CreateTupleDestructuringStatement(methodCall, returns);
// Should create proper tuple destructuring assignment

// Test case: StatementExtractionTarget with single return statement selection
var target = new StatementExtractionTarget([returnStatement], block, semanticModel);
var replacement = target.CreateLocalReturnStatement(methodCall, localSymbol);
// Should create return statement when selection is single return

// Test case: StatementExtractionTarget with method insertion point
var target = new StatementExtractionTarget(statements, block, semanticModel);
var insertionPoint = target.GetInsertionPoint();
// Should find method node or fall back to last statement
```

#### ExpressionExtractionTarget Pattern Matching - DRAFT
Test ExpressionExtractionTarget with specific expression patterns.
```csharp
// Test case: ExpressionExtractionTarget with ToList pattern
var target = new ExpressionExtractionTarget(toListExpression, semanticModel);
var returnType = target.TryInferTypeFromExpression();
// Should return List<string> for .ToList() expressions

// Test case: ExpressionExtractionTarget with Math.Max pattern
var target = new ExpressionExtractionTarget(mathMaxExpression, semanticModel);
var returnType = target.TryInferTypeFromExpression();
// Should return int for Math.Max expressions

// Test case: ExpressionExtractionTarget with method symbol return type
var target = new ExpressionExtractionTarget(methodCallExpression, semanticModel);
var returnType = target.TryInferTypeFromExpression();
// Should use method symbol return type when available

// Test case: ExpressionExtractionTarget with insertion point fallback
var target = new ExpressionExtractionTarget(orphanExpression, semanticModel);
var insertionPoint = target.GetInsertionPoint();
// Should fall back to expression itself when no containing method
```

#### ExtractionTarget Selection Edge Cases - DRAFT
Test ExtractionTarget with complex selection scenarios.
```csharp
// Test case: ExtractionTarget with overlapping statement spans
var target = ExtractionTarget.CreateFromSelection(blockNode, overlappingSpan, block, semanticModel);
// Should correctly identify overlapping statements

// Test case: ExtractionTarget with single statement span containment
var target = ExtractionTarget.CreateFromSelection(statementNode, containingSpan, block, semanticModel);
// Should handle span containment logic correctly

// Test case: ExtractionTarget with expression in descendants
var target = ExtractionTarget.CreateFromSelection(parentNode, expressionSpan, block, semanticModel);
// Should find expression in descendant nodes

// Test case: ExtractionTarget with expression in ancestors
var target = ExtractionTarget.CreateFromSelection(childNode, ancestorSpan, block, semanticModel);
// Should find expression in ancestor nodes

// Test case: ExtractionTarget with equals value clause
var target = ExtractionTarget.CreateFromSelection(equalsValueNode, span, block, semanticModel);
// Should extract value from equals value clause
```
