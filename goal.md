# ExtractMethod Refactoring - Missing Cases and Improvements

## TDD Phase: 🔴

## Open Scenarios

## ExtractMethod Coverage Analysis - Lines Needing Tests

### ExtractMethod Error Cases - ✅ COMPLETED
Test ExtractMethod when no valid extraction target found.
```csharp
// Test case: ExtractMethod with invalid selection
var extractMethod = ExtractMethod.Create(["1:1-1:1"]);
var result = await extractMethod.PerformAsync(emptyDocument);
// Should handle gracefully when no extraction possible
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

### StatementExtractionTarget Complex Return Logic - DRAFT
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

### ExpressionExtractionTarget Pattern Matching - DRAFT
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

### ExtractionTarget Selection Edge Cases - DRAFT
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


## Dead Code Analysis - Lines Needing Tests

### MoveMemberUp Error Handling - DRAFT
Test MoveMemberUp with null/missing scenarios.
```csharp
// Test case: MoveMemberUp when derived class not found
var moveMemberUp = MoveMemberUp.Create(["NonExistentClass", "TestMethod"]);
var result = await moveMemberUp.PerformAsync(document);
// Should return original document when derived class not found (L38-39)

// Test case: MoveMemberUp when base class not found
var moveMemberUp = MoveMemberUp.Create(["DerivedClass", "TestMethod"]);
var result = await moveMemberUp.PerformAsync(documentWithoutBaseClass);
// Should return original document when base class not found (L47-48)

// Test case: MoveMemberUp when member not found
var moveMemberUp = MoveMemberUp.Create(["DerivedClass", "NonExistentMethod"]);
var result = await moveMemberUp.PerformAsync(document);
// Should return original document when member not found (L53-54, L62-63)
```

### InlineMethod Error Handling - DRAFT
Test InlineMethod with null method scenarios.
```csharp
// Test case: InlineMethod when method declaration not found
var inlineMethod = InlineMethod.Create(["1:1"]);
var result = await inlineMethod.PerformAsync(documentWithInvalidMethod);
// Should return null when method declaration not found (L152)

// Test case: InlineMethod when method body is null
var inlineMethod = InlineMethod.Create(["1:1"]);
var result = await inlineMethod.PerformAsync(documentWithNullMethodBody);
// Should return null when method body is null (L161)
```

### TypeInferrer Error Handling - DRAFT
Test TypeInferrer with error type scenarios.
```csharp
// Test case: TypeInferrer with error type in await expression
var inferrer = new TypeInferrer();
var result = inferrer.InferType(awaitExpressionWithErrorType, semanticModel);
// Should return "string" when error type detected (L36-37)

// Test case: TypeInferrer when method block not found
var inferrer = new TypeInferrer();
var result = inferrer.ResolveActualTypeForForeachVariable(localSymbol, containingBlock, semanticModel);
// Should return "var" when method block not found (L95-96)

// Test case: TypeInferrer when foreach statement not found
var inferrer = new TypeInferrer();
var result = inferrer.ResolveActualTypeForForeachVariable(localSymbol, containingBlock, semanticModel);
// Should return "var" when foreach statement not found (L101-102)

// Test case: TypeInferrer when collection type has no type arguments
var inferrer = new TypeInferrer();
var result = inferrer.ExtractElementTypeFromCollection(foreachStatement, semanticModel);
// Should return "var" when collection type has no type arguments (L134)
```

### RenameSymbol Error Handling - DRAFT
Test RenameSymbol with null scope scenarios.
```csharp
// Test case: RenameSymbol when declaration scope not found
var renameSymbol = RenameSymbol.Create(["1:1", "newName"]);
var result = await renameSymbol.PerformAsync(documentWithVariableWithoutScope);
// Should return original document when declaration scope not found (L142)

// Test case: RenameSymbol when document root is null during solution-wide rename
var renameSymbol = RenameSymbol.Create(["1:1", "newName"]);
var result = await renameSymbol.PerformAsync(documentInSolutionWithNullRoots);
// Should continue processing when document root is null (L174)
```

### StatementExtractionTarget Uncovered Error Paths - DRAFT
Test StatementExtractionTarget with error conditions and edge cases.
```csharp
// Test case: StatementExtractionTarget with unsupported return symbol type
var target = new StatementExtractionTarget(statementsWithUnsupportedReturn, block, semanticModel);
var returnType = target.DetermineLocalReturnType(unsupportedReturns);
// Should throw InvalidOperationException for unsupported return symbol type (L131)

// Test case: StatementExtractionTarget with unsupported symbol type in parameters
var target = new StatementExtractionTarget(statementsWithUnsupportedSymbol, block, semanticModel);
var symbolType = target.GetSymbolType(unsupportedSymbol);
// Should throw InvalidOperationException for unsupported symbol type (L236)

// Test case: StatementExtractionTarget with unsupported return in replacement node
var target = new StatementExtractionTarget(statementsWithUnsupportedReturn, block, semanticModel);
var replacement = target.CreateReplacementNode("ExtractedMethod");
// Should throw InvalidOperationException for unsupported return symbol type (L271)

// Test case: StatementExtractionTarget with uncovered lines in method body creation
var target = new StatementExtractionTarget(specificStatements, block, semanticModel);
var methodBody = target.CreateMethodBody();
// Should cover uncovered lines in method body creation logic (L70-71, L97-99, L131, L148, L180-188, L200, L236, L271, L296, L305-306, L348)
```

### ExtractCollaboratorInterface Uncovered Path - DRAFT
Test ExtractCollaboratorInterface with partly covered scenarios.
```csharp
// Test case: ExtractCollaboratorInterface with specific selection handling
var extractor = ExtractCollaboratorInterface.Create(["1:1-2:2"]);
var result = await extractor.PerformAsync(documentWithSpecificCollaborator);
// Should cover partly covered line in selection processing (L33)
```

### ExpressionExtractionTarget Uncovered Paths - DRAFT
Test ExpressionExtractionTarget with specific error conditions.
```csharp
// Test case: ExpressionExtractionTarget with null DataFlow
var target = new ExpressionExtractionTarget(expressionWithNullDataFlow, semanticModel);
var dataFlow = target.AnalyzeDataFlow();
// Should throw InvalidOperationException when DataFlow is null (L14)

// Test case: ExpressionExtractionTarget with error type fallback
var target = new ExpressionExtractionTarget(errorTypeExpression, semanticModel);
var returnType = target.TryInferTypeFromExpression();
// Should fall back to pattern-based inference (L34, L42-48, L52-53, L97)
```

### ReturnBehavior Uncovered Path - DRAFT
Test ReturnBehavior with specific switch statement scenarios.
```csharp
// Test case: ReturnBehavior with switch statement analysis
var behavior = new ReturnBehavior([switchStatementWithAllReturns]);
var requiresReturn = behavior.AllPathsReturnOrThrow;
// Should analyze switch statement sections for return/throw (L20)
```
