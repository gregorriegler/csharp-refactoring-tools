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

## All Code
- NO COMMENTS
- private fields start with a lower case letter, not with underscores
- NO TryXy patterns
- No out/ref variables 
- Use curly braces for single line ifs
- Avoid else if possible
- When a function uses only a derived, or a small percentage of properties of a passed object, pass the specific elements instead.
- Each class goes into its own file, unless its only used by the other class in the file and it both fits into 100 lines
- Only ever use file-scoped namespaces
- Prefer collection expression over new List

## Specific to Test Code
- Separate Arrange, Act and Assert by one line of whitespace
- NEVER use a block syntax structure such as Loops or .Where() in a Test. The test has only one path and it knows the expected outcome. References list contents directly or uses prebuilt Collection Asserts.
- Don't use Assert.Multiple. Each Assert stands on its own on its own line.
