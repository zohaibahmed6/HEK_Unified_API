using Xunit;

// The LiveIntegration suite hits the real API's RateLimit middleware (appsettings.json:
// PermitLimit=30/WindowSeconds=60) - xUnit's default parallel test execution blows through that in
// seconds and every test after the first ~30 gets a false-negative 429, not a real bug. Force
// sequential execution for the whole assembly so these tests behave like the manual, one-at-a-time
// verification they're standing in for.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
