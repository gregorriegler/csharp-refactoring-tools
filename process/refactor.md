# Refactoring Process

STARTER_SYMBOL=🧹

The goal is to identify a small step towards an improvement in the design. 
Aim for better maintainability but avoid overengineering. 
Favor polymorphism over repeated conditions.
Favor value objects over tuples and other primitive data structures.
Favor tell don't ask over properties.
Avoid interfaces for stable dependencies.
Avoid booleans as arguments.
Avoid redundant parameters and making callers provide information the method can derive itself.

1. Initiate a new subtask to analyze the given code and find a small step that improves the design. Don't implement the change, just report back the result of the analysis.
2. Initiate a new subtask to decompose the proposed design improvement to a plan of many small refactoring steps - when the plan is ready, close the task reporting back the plan.
3. Execute the planned refactoring steps, creating a new subtask for each step where you run the tests before and after the changes using `./test.sh` and commit the changes using a commit message "r <message>"
