#!/bin/bash

# approve.sh - Approve test results by copying .received.txt/.received.md to .verified.txt/.verified.md files
# Usage: ./approve.sh [test_name_pattern]
# Example: ./approve.sh RenameSymbol
# Example: ./approve.sh CanRenameUnusedLocalVariable

set -e

# Function to display usage
show_usage() {
    echo "Usage: $0 [test_name_pattern]"
    echo ""
    echo "Approve test results by copying .received.txt/.received.md files to .verified.txt/.verified.md files"
    echo ""
    echo "Examples:"
    echo "  $0                           # Approve all received files"
    echo "  $0 RenameSymbol             # Approve all RenameSymbol test files"
    echo "  $0 CanRenameUnusedLocal     # Approve specific test"
    echo ""
    echo "The script will:"
    echo "  1. Find all .received.txt/.received.md files matching the pattern"
    echo "  2. Copy each .received file to its corresponding .verified file"
    echo "  3. Delete the .received file after successful copy"
}

# Check if help is requested
if [[ "$1" == "-h" || "$1" == "--help" ]]; then
    show_usage
    exit 0
fi

# Get the test name pattern (optional)
TEST_PATTERN="$1"

# Find the refactoring-tools directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REFACTORING_DIR="$SCRIPT_DIR"

# If we're not in refactoring-tools, try to find it
if [[ ! -d "$REFACTORING_DIR/RoslynRefactoring.Tests" && ! -d "$REFACTORING_DIR/RoslynAnalysis.Tests" ]]; then
    # Try parent directories
    PARENT_DIR="$(dirname "$SCRIPT_DIR")"
    if [[ -d "$PARENT_DIR/refactoring-tools" ]]; then
        REFACTORING_DIR="$PARENT_DIR/refactoring-tools"
    else
        echo "Error: Could not find refactoring-tools directory with test projects"
        echo "Please run this script from the refactoring-tools directory or its parent"
        exit 1
    fi
fi

echo "Working in directory: $REFACTORING_DIR"

# Build the find patterns for both .txt and .md files
if [[ -n "$TEST_PATTERN" ]]; then
    FIND_PATTERN_TXT="*${TEST_PATTERN}*.received.txt"
    FIND_PATTERN_MD="*${TEST_PATTERN}*.received.md"
    echo "Looking for received files matching patterns: $FIND_PATTERN_TXT and $FIND_PATTERN_MD"
else
    FIND_PATTERN_TXT="*.received.txt"
    FIND_PATTERN_MD="*.received.md"
    echo "Looking for all received files: $FIND_PATTERN_TXT and $FIND_PATTERN_MD"
fi

# Find all .received.txt and .received.md files matching the pattern
RECEIVED_FILES_TXT=$(find "$REFACTORING_DIR" -name "$FIND_PATTERN_TXT" -type f 2>/dev/null || true)
RECEIVED_FILES_MD=$(find "$REFACTORING_DIR" -name "$FIND_PATTERN_MD" -type f 2>/dev/null || true)

# Combine both results
RECEIVED_FILES=$(printf "%s\n%s" "$RECEIVED_FILES_TXT" "$RECEIVED_FILES_MD" | grep -v '^$' || true)

if [[ -z "$RECEIVED_FILES" ]]; then
    if [[ -n "$TEST_PATTERN" ]]; then
        echo "No .received.txt/.received.md files found matching patterns: $FIND_PATTERN_TXT and $FIND_PATTERN_MD"
    else
        echo "No .received.txt/.received.md files found matching patterns: $FIND_PATTERN_TXT and $FIND_PATTERN_MD"
    fi
    echo ""
    echo "This could mean:"
    echo "  - All tests are passing (no received files generated)"
    echo "  - The pattern doesn't match any files"
    echo "  - Tests haven't been run yet"
    echo ""
    echo "To generate .received files, run failing tests first:"
    echo "  dotnet test"
    exit 0
fi

echo "Found received files:"
echo "$RECEIVED_FILES"
echo ""

# Process each received file
APPROVED_COUNT=0
FAILED_COUNT=0

while IFS= read -r received_file; do
    if [[ -z "$received_file" ]]; then
        continue
    fi

    # Generate the corresponding verified file name
    if [[ "$received_file" == *.received.txt ]]; then
        verified_file="${received_file%.received.txt}.verified.txt"
    elif [[ "$received_file" == *.received.md ]]; then
        verified_file="${received_file%.received.md}.verified.md"
    else
        echo "  ⚠ Warning: Unknown file extension for $received_file"
        continue
    fi

    echo "Approving: $(basename "$received_file")"
    echo "  From: $received_file"
    echo "  To:   $verified_file"

    # Copy received to verified
    if cp "$received_file" "$verified_file"; then
        echo "  ✓ Copied successfully"

        # Remove the received file
        if rm "$received_file"; then
            echo "  ✓ Removed received file"
            ((APPROVED_COUNT++))
        else
            echo "  ⚠ Warning: Could not remove received file"
            ((APPROVED_COUNT++))
        fi
    else
        echo "  ✗ Failed to copy file"
        ((FAILED_COUNT++))
    fi
    echo ""
done <<< "$RECEIVED_FILES"

# Summary
echo "=== Summary ==="
echo "Approved: $APPROVED_COUNT files"
if [[ $FAILED_COUNT -gt 0 ]]; then
    echo "Failed: $FAILED_COUNT files"
    exit 1
else
    echo "All files processed successfully!"
fi
