# Copilot Instructions for MyMarketManager

## Project Overview

MyMarketManager is a mobile and web application for managing weekend market operations. The application handles:
- Supplier purchase orders and deliveries
- Inventory reconciliation
- Sales imports
- Profitability analysis

## Technology Stack

- **Framework**: .NET 10
- **Platform**: Cross-platform (mobile and web)
- **Primary Language**: C#

## Development Setup

This repository uses .NET 10. The setup is automated in `.github/workflows/copilot-setup-steps.yml`.

## Coding Standards

### General Guidelines

- Follow C# coding conventions and best practices
- Use meaningful variable and method names
- Write clear, concise comments for complex logic
- Keep methods focused and single-purpose

### .NET Specific

- Use modern C# features appropriately (pattern matching, null-coalescing, etc.)
- Follow async/await patterns for asynchronous operations
- Use dependency injection for loose coupling
- Implement proper error handling and logging

### Code Organization

- Keep related functionality together
- Use namespaces to organize code logically
- Separate concerns (business logic, data access, presentation)

## Testing

- Write unit tests for new functionality
- Ensure tests are maintainable and readable
- Mock external dependencies appropriately
- Aim for meaningful test coverage, not just high percentages

## .NET Tools

- Always use local .NET tools over globally installed tools
- If a new tool is needed, install locally unless specifically requested.
- If a tool is requested to not install locally, edit the `.github/workflows/copilot-setup-steps.yml` file to install it globally.

## Documentation

- Update README.md when adding major features
- Document public APIs with XML documentation comments
- Keep inline comments focused on "why" rather than "what"

## Git Workflow

- Write clear, descriptive commit messages
- Keep commits focused and atomic
- Reference issue numbers in commit messages when applicable

## License

This project is licensed under the MIT License. See the LICENSE file for details.
