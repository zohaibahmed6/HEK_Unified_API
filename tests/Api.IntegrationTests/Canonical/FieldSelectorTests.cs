using FluentAssertions;
using HekCoreApi.Api.Features.Canonical;

namespace Api.IntegrationTests.Canonical;

/// <summary>
/// FieldSelector is the core FR-4/FR-5 enforcement point (unified spec §3/§6): a consumer only gets
/// fields it both asked for and is scoped to. These tests lock in that intersection behaviour
/// without needing the full HTTP pipeline.
/// </summary>
public sealed class FieldSelectorTests
{
    private sealed record Sample(int PatientId, string FirstName, string LastName, string? Secret);

    private static readonly string[] AllowedFields = { nameof(Sample.PatientId), nameof(Sample.FirstName), nameof(Sample.LastName) };

    [Fact]
    public void Project_ReturnsAllAllowedFields_WhenNoFieldsRequested()
    {
        var sample = new Sample(1, "Amy", "Mouse", "hidden");

        var result = FieldSelector.Project(sample, requestedFields: null, AllowedFields);

        result.Keys.Should().BeEquivalentTo("patientId", "firstName", "lastName");
        result.Should().NotContainKey("secret");
    }

    [Fact]
    public void Project_IntersectsRequestedAndAllowed_WhenFieldsRequested()
    {
        var sample = new Sample(1, "Amy", "Mouse", "hidden");

        var result = FieldSelector.Project(sample, requestedFields: ["firstName"], AllowedFields);

        result.Should().ContainKey("firstName").WhoseValue.Should().Be("Amy");
        result.Should().NotContainKey("lastName");
        result.Should().NotContainKey("patientId");
    }

    [Fact]
    public void Project_SilentlyDropsRequestedField_WhenOutsideAllowedScope()
    {
        var sample = new Sample(1, "Amy", "Mouse", "hidden");

        var result = FieldSelector.Project(sample, requestedFields: ["secret", "firstName"], AllowedFields);

        result.Should().NotContainKey("secret");
        result.Should().ContainKey("firstName");
    }
}
