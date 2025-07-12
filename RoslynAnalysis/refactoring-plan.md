# Refactoring Plan: Extract Method - Break Down Long Method

## Goal
Refactor the `ExtractMethod.PerformAsync` method (335 lines) into smaller, more cohesive methods with single responsibilities.

## Steps

- [ ] Remove debug Console.WriteLine statements to clean up the code
- [ ] Extract method to find selected statements and expressions from the span
- [ ] Extract method to analyze data flow and determine parameters/returns
- [ ] Extract method to determine return type for the new method
