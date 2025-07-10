# Write a Failing Test

STARTER_SYMBOL = 🔴

Your answers should be succinct and concise.

1. Understand where you are
    - Read the `README.md` and `goal.md`.
    - Run `pwd`.
2. Ensure there are no uncommitted changes
    - **STOP** immediately if uncommitted changes are detected. use `./say.py` to notify me
3. Confirm all current tests pass by executing `./test.sh`
    - Proceed only if all tests pass. If they don't stop and notify me using `./say.py`
5. Think: Will this example already work given the current production code? Do we already have testcode forcing this behavior?
6. If the answer two both is yes, then we don't have to add any code. Check off the item in `goal.md` and proceed with 3. 
7. Otherwise, write a failing test
    - It should be the simplest possible test that demonstrates that the feature does not yet exist.
    - The test must use domain-specific language and avoid talking implementation details.
8. Hypothesize the test outcome
    - Clearly state your expectation: Will the test fail? Why?
9. Run the test using `./test.sh` and observe the outcome
    - **Note**: Verify or ApprovalTests will fail initially because they require approval.
10. If it surprisingly already passes, or the received.txt is already what it should be, we approve it using `./approve.sh`. We then commit with the message "t ..."
11. Only if we have a failing test to make pass, end this task with the message: "Added a failing Test"
12. Otherwise continue from 2.
