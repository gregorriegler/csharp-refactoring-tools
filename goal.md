# ExtractMethod Refactoring - Missing Cases and Improvements

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
