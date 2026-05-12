# 02.02-remove-deprecated-package: Remove deprecated FluentValidation.AspNetCore and update FluentValidation references

# 02.02-remove-deprecated-package: Handle FluentValidation.AspNetCore deprecation

## Objective
Remove the deprecated FluentValidation.AspNetCore 11.3.1 package from Web.csproj and replace with recommended alternative or remove if no replacement needed.

## Context
Assessment flagged FluentValidation.AspNetCore as deprecated (NuGet.0005). Current version in Web.csproj is 11.3.1. FluentValidation.DependencyInjectionExtensions 12.1.1 is already referenced and provides the core functionality.

## Done when
FluentValidation.AspNetCore is removed from Web.csproj, fluentvalidation still builds, no compilation errors in code using FluentValidation.
