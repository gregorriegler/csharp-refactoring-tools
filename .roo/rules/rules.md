# Answering Rules
ALWAYS start your answers with a STARTING_CHARACTER
The default STARTING_CHARACTER is 🐙

Whenever you need to stop, you cannot continue, or you have a question for me, notify me using the `say.py` script using `./say.py "<your message>"

I prefer SHORT, SUCCINCT and CONCISE answers

Don't be overly optimistic, but be critical and skeptical. 

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

## Specific to Test Code
- Separate Arrange, Act and Assert by one line of whitespace
- NEVER use Loops or .Where() in a Test. The test knows the expected outcome and references list contents directly or uses prebuilt Collection Asserts.
- Don't do Assert.Multiple. Each Assert stands on its own on its own line.
