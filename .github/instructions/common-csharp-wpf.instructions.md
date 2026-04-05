---
applyTo: "**/*.{cs,xaml,csproj,slnx,md}"
description: "Common reusable rules for C# and WPF projects: maintainability, MVVM, dependency injection, testing, documentation, logging, packages, and quality gates."
---

# Common C# / WPF Rules

These rules are intended to be reusable across C# and WPF projects.

## 1. Coding Standards

### Core Principles
- Always prioritize maintainability in every change.
- Favor readable, testable, loosely coupled code with clear responsibilities.
- Prefer small classes and methods with a single responsibility.
- Avoid duplication by extracting reusable abstractions where appropriate.

### Naming Conventions
- Use PascalCase for class names, method names, property names, and namespaces.
- Use camelCase for local variables, method parameters, and private fields.
- Prefix private fields with an underscore.
- Follow Microsoft's C# naming guidelines.

### Code Style
- Use 4 spaces for indentation.
- Limit line length to 120 characters when practical.
- Use meaningful names and avoid unclear abbreviations.
- Group related code with blank lines for readability.

### File Organization
- Place each class in its own file.
- Keep folder structure aligned with architectural responsibilities.

## 2. Architecture

### WPF Guidelines
- Implement MVVM for separation of concerns.
- Use data binding in XAML.
- Avoid business logic in code-behind.
- Use INotifyPropertyChanged or an ObservableObject base for UI state.

### General Design
- Follow SOLID principles.
- Prefer interfaces where test seams are needed.
- Use async and await for I/O and UI responsiveness.
- Keep infrastructure concerns separate from business logic.

## 3. Error Handling and Logging
- Handle exceptions at the appropriate layer.
- Avoid broad catch blocks unless they are the intended boundary.
- Provide meaningful user-facing error messages.
- Include enough context in logs to diagnose failures.

## 4. Testing
- Add unit tests for business logic and orchestration logic.
- Create or update tests in the same change as production code.
- Keep test names aligned with the target class name.
- Cover normal flow, fallback flow, and error flow where applicable.

## 5. Documentation
- Add XML comments for public APIs.
- Keep comments accurate when names or responsibilities change.
- Keep project documentation current when architecture or behavior changes.

## 6. Dependencies and Quality
- Use NuGet intentionally and avoid unnecessary dependencies.
- Pin package versions where stability matters.
- Run static analysis and tests as part of normal verification.
- Keep license documentation aligned when dependencies change.