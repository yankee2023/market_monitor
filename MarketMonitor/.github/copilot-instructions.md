# Copilot Instructions for C# Application Development
<!-- C#アプリケーション開発のためのCopilot指示 -->

This document outlines the guidelines and best practices for developing C# applications, specifically for WPF-based projects like MarketMonitor. GitHub Copilot should adhere to these instructions when generating code, suggestions, or refactoring.
<!-- このドキュメントは、MarketMonitorのようなWPFベースのC#アプリケーション開発におけるガイドラインとベストプラクティスを概説しています。GitHub Copilotは、コード生成、提案、リファクタリング時にこれらの指示に従ってください。 -->

## 1. Coding Standards
<!-- コーディング標準 -->

### Core Principles
<!-- 基本原則 -->
- Always prioritize maintainability in every change.
- Always design with object-oriented principles in mind.
- Favor readable, testable, loosely coupled code with clear responsibilities.
- Prefer small classes and methods with a single responsibility.
- Avoid duplication by extracting reusable abstractions where appropriate.

### Naming Conventions
<!-- 命名規則 -->
- Use PascalCase for class names, method names, property names, and namespaces.
- Use camelCase for local variables, method parameters, and private fields.
- Prefix private fields with an underscore (e.g., `_privateField`).
- Use UPPER_CASE for constants.
- Follow Microsoft's C# naming guidelines.

### Code Style
<!-- コードスタイル -->
- Use 4 spaces for indentation (no tabs).
- Limit line length to 120 characters.
- Use meaningful variable and method names; avoid abbreviations unless widely accepted.
- Group related code with blank lines for readability.

### File Organization
<!-- ファイル構成 -->
- Place each class in its own file.
- Use folders to organize code by functionality (e.g., Views, ViewModels, Models, Services).
- Follow the project's folder structure consistently.

## 2. Architecture and Design Patterns
<!-- アーキテクチャとデザインパターン -->

### WPF-Specific Patterns
<!-- WPF特有のパターン -->
- Implement MVVM (Model-View-ViewModel) pattern for separation of concerns.
- Use data binding extensively in XAML.
- Avoid code-behind in Views; move logic to ViewModels.
- Use Dependency Properties for custom controls.
- Implement INotifyPropertyChanged or use ObservableObject for data binding.

### General Architecture
<!-- 一般的なアーキテクチャ -->
- Follow SOLID principles.
- Use dependency injection for loose coupling.
- Prefer interfaces over concrete implementations.
- Implement asynchronous programming with async/await for UI responsiveness.

## 3. Error Handling and Logging
<!-- エラーハンドリングとログ -->
- Use try-catch blocks appropriately; avoid catching general exceptions.
- Implement logging using frameworks like Serilog or NLog.
- Provide meaningful error messages to users.
- Handle exceptions at the appropriate layer (UI, business logic, data access).
- For error logs, include detailed information to identify the cause (exception message, stack trace, relevant data, API responses, etc.).

## 4. Testing
<!-- テスト -->
- Write unit tests for all business logic using xUnit or NUnit.
- Use mocking frameworks like Moq for dependencies.
- Aim for high code coverage (target 80%+).
- Write integration tests for critical paths.
- Use Test-Driven Development (TDD) where possible.
- Create unit tests in a separate project (e.g., [ProjectName].Tests).
- When adding classes or methods, create corresponding unit tests simultaneously.
- Name test files as [CorrespondingClassName]Test (e.g., ApiServiceTest.cs).
- Name test projects as [CorrespondingProjectName]Test (e.g., MarketMonitor.ConsolePoCTest).

## 5. Security
<!-- セキュリティ -->
- Validate all user inputs to prevent injection attacks.
- Use parameterized queries for database operations.
- Implement authentication and authorization as needed.
- Avoid storing sensitive data in plain text.
- Follow OWASP guidelines for web-related features.

## 6. Performance
<!-- パフォーマンス -->
- Optimize LINQ queries; use compiled queries for repeated operations.
- Avoid unnecessary object allocations in loops.
- Use StringBuilder for string concatenations in loops.
- Implement lazy loading for large data sets.
- Profile and optimize UI rendering in WPF.

## 7. Documentation
<!-- ドキュメント -->
- Use XML comments for all public APIs (classes, methods, properties).
- Document complex logic with inline comments.
- Maintain up-to-date README.md and API documentation.
- Use meaningful commit messages following conventional commits.
- Write comments in Japanese.
- Always include appropriate comments for classes and methods. Update comments if method names, parameters, or responsibilities change.
- For test methods, include descriptions of what is being tested and expected values.
- Use UML diagrams or equivalent visual models in design documentation to express architecture, component relationships, sequence flow, and data structure design.
- For sequence diagrams, distinguish synchronous and asynchronous messages.
- For sequence diagrams, keep the flow easy to follow and avoid including log output; focus on the core processing steps.
- For class diagrams, distinguish association lines and dependency lines.
- For class diagrams, always include multiplicity on associations (omit multiplicity only when the value is 1).
- Use clear labels, avoid crossing lines, and keep diagrams minimal and readable.

## 8. Version Control
<!-- バージョン管理 -->
- Use Git for version control.
- Follow the .gitignore rules strictly.
- Use feature branches for development.
- Require code reviews for all merges to main branch.

## 9. Build and Deployment
<!-- ビルドとデプロイ -->
- Use MSBuild or dotnet CLI for building.
- Implement continuous integration with GitHub Actions or Azure DevOps.
- Package applications using ClickOnce or MSIX for WPF apps.
- Ensure builds are reproducible and deterministic.

## 10. Dependencies and Packages
<!-- 依存関係とパッケージ -->
- Use NuGet for package management.
- Pin package versions to ensure stability.
- Regularly update dependencies for security patches.
- Avoid unnecessary dependencies.
- When adding or updating external libraries, also update the third-party license markdown file and any README reference to it.
- If license rules require source code changes, make the necessary code updates as part of the change and insert a comment that clearly indicates the change is for license compliance.

## 11. Code Quality Tools
<!-- コード品質ツール -->
- Use Roslyn analyzers for code quality.
- Integrate StyleCop or similar for style enforcement.
- Run static analysis tools regularly.

## 12. WPF-Specific Guidelines
<!-- WPF特有のガイドライン -->
- Use XAML for UI definition; keep it clean and readable.
- Bind to ViewModels, not code-behind.
- Use converters for complex data transformations in XAML.
- Implement proper resource management (e.g., IDisposable).
- Optimize for DPI awareness and accessibility.

## 13. Database and Data Access
<!-- データベースとデータアクセス -->
- Use Entity Framework Core for ORM.
- Implement migrations for schema changes.
- Use repository pattern for data access.
- Ensure connection strings are securely managed.

## 14. Internationalization and Localization
<!-- 国際化とローカライズ -->
- Design UI for localization from the start.
- Use resource files for strings.
- Support right-to-left languages if applicable.

## 15. Maintenance and Refactoring
<!-- メンテナンスとリファクタリング -->
- Regularly refactor code to improve maintainability.
- Remove dead code and unused dependencies.
- Follow the Boy Scout Rule: leave code better than you found it.

These instructions should be followed consistently across the project. If any conflicts arise with existing code, prioritize refactoring to align with these guidelines.
<!-- これらの指示はプロジェクト全体で一貫して従ってください。既存のコードと競合する場合、これらのガイドラインに合わせてリファクタリングを優先してください。 -->
