# Refactoring Process

STARTER_SYMBOL=🧹

The goal is to identify a small step towards an improvement in the design. 
Aim for better maintainability but avoid overengineering. 
Favor polymorphism over repeated conditions.
Favor value objects over tuples and other primitive data structures.
Favor tell don't ask over properties.
Avoid interfaces for stable dependencies.
Avoid booleans as arguments.

1. Initiate a fresh context to come up with a small step that improves the design.
2. In a fresh context, decompose the change to the proposed design into a plan that consists only of small refactoring steps - this context should just report back the refactoring plan.
3. Execute the refactoring steps, creating a fresh context for each step where you run the tests before and after the changes using `./test.sh` and commit the changes using a commit message "r <message>"
