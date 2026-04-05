# Copilot Instructions for Tokyo Market Technical

This file contains project-specific rules for Tokyo Market Technical.

Reusable C# and WPF guidance has been moved to [.github/instructions/common-csharp-wpf.instructions.md](.github/instructions/common-csharp-wpf.instructions.md).

## 1. Scope
- This application is Japan-stock-only.
- Remove or reject functionality for exchange rates and non-Japanese markets unless the specification explicitly changes.
- User-facing symbol handling must resolve to Tokyo `.T` symbols and company names listed on Tokyo Prime, Standard, or Growth.

## 2. Source of Truth
- Keep [SPECIFICATION.md](SPECIFICATION.md), [DESIGN.md](DESIGN.md), and implementation aligned in the same change.
- Treat [SPECIFICATION.md](SPECIFICATION.md) as the requirements source of truth.
- Treat [DESIGN.md](DESIGN.md) as the design source of truth.
- When behavior changes, update documentation before or together with code.
- Preserve the Copilot waterfall workflow and templates in [docs/COPILOT_WATERFALL_WORKFLOW.md](docs/COPILOT_WATERFALL_WORKFLOW.md), [templates/DESIGN_FROM_SPEC_TEMPLATE.md](templates/DESIGN_FROM_SPEC_TEMPLATE.md), and [templates/IMPLEMENTATION_FROM_DESIGN_TEMPLATE.md](templates/IMPLEMENTATION_FROM_DESIGN_TEMPLATE.md).

## 3. Architecture
- Keep the feature-sliced structure centered on `Composition`, `Shared`, and `Features`.
- Put cross-feature concerns only under `Shared`.
- Keep orchestration for the main screen in `Features/Dashboard/ViewModels/MainViewModel`.
- Keep `Composition` limited to object wiring and startup configuration.
- Do not reintroduce the removed legacy root-level architecture.

## 4. Data Sources and Persistence
- Use Yahoo Finance as the primary source for Japanese stock price and candle data.
- Use Stooq only as a fallback.
- Use JPX listed-company data for Tokyo Prime, Standard, and Growth name resolution.
- Keep SQLite history schema limited to `symbol`, `stock_price`, and `recorded_at`.
- When dependency changes affect licensing, update [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md) and the related README reference.

## 5. Logging and Errors
- Use Serilog and keep file output under `logs/app-.log`.
- Keep user-facing error messages in Japanese.
- Keep shared market-data error messages centralized.

## 6. Tests and Quality Gates
- Prefer xUnit for tests in this repository.
- Add tests when changing feature orchestration, fallbacks, repositories, or symbol resolution logic.
- When changing XAML resources, templates, or merged dictionaries, add or update a regression test that loads the target ResourceDictionary or constructs the target Window on STA to catch missing StaticResource and merge-order failures.
- Maintain or improve the enforced coverage threshold in `MarketMonitorTest`.
- Prefer test seams such as interfaces or injected collaborators over fragile reflection-based tests when changing production code.

## 7. Project Conventions
- Write comments in Japanese.
- Keep public XML comments current when responsibilities change.
- Preserve the current naming and folder conventions unless the specification changes.
