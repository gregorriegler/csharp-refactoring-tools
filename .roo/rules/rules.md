# Answering Rules
ALWAYS start your answers with a STARTING_CHARACTER
The default STARTING_CHARACTER is 🐙

Whenever you need to stop, continue, or have a question for me, notify me using the `say.py` script using `./say.py "<your message>"

I prefer SHORT and SUCCINCT answers

Don't be optimistic, but be critical and skeptical.
Hypothesize first and prove your hypothesis before taking action. 

# Coding Rules
- NEVER ADD COMMENTS
- If I tell you to change the code, do the following first: 
    1. Run `git status`, we do not start with a dirty git status
    2. Run `test.sh`, we do not start with failing tests
- AFTER THE TASK: run `test.sh` again

# Commandline rules
- We are in a Git Bash!!! ALWAYS USE BASH Commands!!!
- for interacting with github use the github cli

# CSharp Style Guide

## Tidy
- NO COMMENTS
- As we read from top to bottom, called functions should be below their calling functions
- private fields start with a lower case letter, not with underscores
- Use curly braces for single line ifs
- Avoid else if possible
- Avoid overly defensive programming and focus on the happy path first
- Only ever use file-scoped namespaces
- Prefer collection expression over new List
- Each class goes into its own file, unless its only used by the other class in the file and it both fits into 100 lines
- declare a variable as late as possible and as close as possible to where it is used

## Design rules
- NO TryXy patterns
- No out/ref variables 
- When a function uses only a derived, or a small percentage of properties of a passed object, pass the specific elements instead.
- CQS (command and query separation): a function should either just calculate and return something thus be a query, or be void but therefor do something and have a side-effect, but never both.

## Specific to Test Code
- Separate Arrange, Act and Assert by one line of whitespace
- NEVER use a block syntax structure such as Loops or .Where() in a Test. The test has only one path and it knows the expected outcome. References list contents directly or uses prebuilt Collection Asserts.
- Don't use Assert.Multiple. Each Assert stands on its own on its own line.
- Test readability trumps code reuse!  
  - Keep test data inline when the data structure IS what's being tested.
