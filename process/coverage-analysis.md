# Code Coverage Analysis

STARTER_SYMBOL=📊

Goal of this process is to identify uncovered code and decided what to do with it.
Uncovered code is technically code without tests.
Since all code is driven by tests, we consider code with tests superficial.
It might as well be code that is infrastructure code that is not testable, and we decided to not test it. Some uncovered code makes sense to keep and add tests for.

1. Run `./coverage.sh` to find uncovered lines.
2. Focus on the lines we should remove. Remove them keeping the tests `./tests.sh` passing
3. Address the lines lines we should add tests for. Insert one simple as possible example for each uncovered line, that would cover it to the `goal.md` as the next to implement.
4. End this task with just one sentence, nothing more: "Coverage analysis completed."
