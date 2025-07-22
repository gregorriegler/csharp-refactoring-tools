# Remove Dead Code

STARTER_SYMBOL=💀

Goal of this process is to identify uncovered code and decide what to do with it.
Since all code is driven by tests, we consider code without tests superficial a.k.a. Dead Code.
Only when its infrastructure code that is not testable, and is not tested on purpose, we keep it. This is often the main program entrypoint. 
Some uncovered code makes sense to keep and add tests for, it is up for you to decide

1. Run `./coverage.sh` to find uncovered lines.
2. Focus on the lines we should remove. Remove them keeping the tests `./tests.sh` passing
3. If the tests pass after removing dead code, git commit with the message "r dead code"
4. Address the lines lines we should add tests for. Insert one simple as possible example for each uncovered line, that would cover it to the `goal.md` as the next to implement. Commit the change with the git message "d <message>"
5. End this task with just one sentence, nothing more: "Coverage analysis completed."
