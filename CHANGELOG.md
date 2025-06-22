# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Fixed
- Fixed compilation errors in unit tests related to token services
  - Updated `TokenService` constructor calls to match the actual implementation
  - Aligned `RefreshTokenCommand` usage with its current signature
  - Fixed event publishing tests to use `TokenGeneratedEvent` instead of non-existent `TokenRefreshedEvent`
  - Added missing `CancellationToken` parameters to async method calls
  - Fixed null reference warnings in test assertions
  - Updated test mocks to match the current implementation
  - Fixed xUnit2002 warnings by removing `Assert.NotNull` calls on value types
  - Added XML documentation to public test methods to resolve documentation warnings

### Changed
- Improved null safety in test methods by using nullable reference types
- Updated test assertions to be more precise and avoid potential null reference exceptions
- Enhanced test method documentation for better maintainability
