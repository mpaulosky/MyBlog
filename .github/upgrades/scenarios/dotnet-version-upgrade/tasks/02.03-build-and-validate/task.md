# 02.03-build-and-validate: Build solution, fix all warnings, run full test suite

# 02.03-build-and-validate: Build, fix warnings, validate tests

## Objective

Build the entire solution with all retargeted projects, fix all warnings in projects modified, and run the full test suite to validate functionality on net11.0.

## Context

After retargeting and package changes, the solution must build cleanly and all tests must pass. Assessment identified 3 potential API compatibility issues that may appear as compile errors or test failures during this step.

## Done when

Solution builds with zero errors and zero warnings, all tests pass on net11.0.
