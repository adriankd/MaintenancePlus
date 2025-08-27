# Unit Testing Development Plan

## Objectives
- Establish a reliable automated testing setup aligned with PRD Section 12 (QA & Testing)
- Prioritize unit tests for numeric parsing, JSON extraction/cleaning, classification fallback, and service-layer logic
- Achieve fast feedback locally and in CI with coverage visibility for critical parsing paths

## Scope (Phase 1)
- Unit tests for:
  - Numeric parsing utilities: odometer and currency with comma formats
  - JSON extraction from markdown (GitHubModelsService.ExtractJsonFromMarkdown)
  - Description summary post-processing helpers (if present)
  - Rule-based fallback classification logic (keywords)
  - Minimal Controller tests for basic validation and edge guards (lightweight)
- Exclude for now: Full EF Core integration tests, blob storage, external HTTP (mock instead)

## Tools & Frameworks
- Test framework: xUnit.net
- Assertions: FluentAssertions
- Mocking: NSubstitute (or Moq) + Microsoft.Extensions.Logging.Abstractions
- HTTP mocking: RichardSzalay.MockHttp for HttpClient
- Coverage:
  - Coverlet data collector (dotnet test /collect:"XPlat Code Coverage")
  - ReportGenerator to produce HTML summary (optional for local)
- Test helpers:
  - AutoFixture for generating inputs (optional)

## Project Layout
- Create a new test project: `tests/VehicleMaintenanceInvoiceSystem.Tests/VehicleMaintenanceInvoiceSystem.Tests.csproj`
- Keep production code under `src/`
- Add Directory.Build.props to enforce nullable, langversion, and consistent analyzers for tests

## Initial Test Backlog (from PRD requirements)
1) Numeric Extraction (Priority P0)
   - Parse "67,890" -> 67890
   - Parse "1,234,567" -> 1234567
   - Trim spaces: " 123,456 " -> 123456
   - Invalid input -> returns null or error path handled
   - Currency with separators and decimals
2) JSON Extraction (P0)
   - ExtractJsonFromMarkdown: JSON fenced with ```json
   - JSON block inside prose with extra backticks and stray text
   - No JSON present -> returns null and logs warning (do not leak content)
3) Fallback Classification (P1)
   - Keyword exact/contains/partial matching picks category consistently
   - Case-insensitive matching
   - Unmatched -> Other/Labor default based on current implementation
4) GitHubModelsService happy-path shims (P2)
   - Minimal test that verifies we don’t log raw bodies on error; use fake handler returning 429/500 and assert only length logged (via custom logger sink)
5) Controller Guards (P3)
   - Upload rejects >10MB
   - Invalid file types rejected

## Contracts & Edge Cases
- Numeric parsing
  - Input: string; Output: int/decimal?; Reject invalid; handle commas; culture-invariant
  - Edge: empty/null; very large; whitespace; mixed non-digits
- JSON extraction
  - Input: string; Output: string (JSON) or null; Must not throw on uneven backticks
  - Edge: multiple code blocks; nested backticks; markdown tables
- Fallback classification
  - Input: description; Output: enum/category; Deterministic tie-break

## Mocks and Isolation Strategy
- Replace HttpClient with mocked HttpMessageHandler
- Replace ILogger<T> using LoggerFactory with custom sink to assert messages without PII
- Avoid touching database; if logic requires DbContext, abstract behind interface and substitute

## CI Integration
- Add a GitHub Actions workflow in a follow-up PR to run:
  - dotnet restore; dotnet build; dotnet test --collect:"XPlat Code Coverage"
  - Upload coverage artifact; optionally generate HTML via ReportGenerator

## Milestones
- M1: Scaffolding test project + first numeric parsing tests green
- M2: JSON extraction tests + logger guard tests
- M3: Fallback classification tests
- M4: Coverage threshold checks (60% initial, aiming higher over time)

## Risks/Assumptions
- Assumption: Parsing helpers exist or are easy to factor out; if embedded, we’ll extract minimal static helpers for testability (no behavior change)
- External services will be mocked; no secrets required
- Time-box HTTP integration tests; keep unit tests < 200ms each

## Next Steps
- Create test project scaffold under `tests/`
- Add packages: xunit, xunit.runner.visualstudio, FluentAssertions, NSubstitute, RichardSzalay.MockHttp, coverlet.collector
- Implement Numeric and JSON extraction tests first
- Wire coverage collection and document run commands in README
