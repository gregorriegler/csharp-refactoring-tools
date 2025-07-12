## Plan a refactoring

STARTER_SYMBOL=🧹

1. If there is already a `refactoring-plan.md` file, clean it so it's empty.
2. Find out what files were changed in the last 5 commits.
3. In those files find one thing to improve. E.g.:
    - Remove Comments
    - Remove Dead code
    - Remove production code that is not exercised by any tests. If you are unsure whether you can remove production code, run a [mutation test](./mutation-test.md)
    - Feature Envy
    - Extract duplicated code
    - Refactor Long Methods
      - Note: When you extract something, it should not have more than one return value. Too many return values as well as too many arguments are an indicator for strong coupling. Prefer high cohesion and low coupling, where high cohesion comes first.
    - Primitive Obsession
4. Think, whether the thing you want to improve can be decomposed into small steps that leave the tests passing.
5. List all the steps as tasks prefixed with a checkbox in `refactoring-plan.md`.
