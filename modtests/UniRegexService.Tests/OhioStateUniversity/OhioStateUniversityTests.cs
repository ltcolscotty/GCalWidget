using GCaLink.Services;
using Xunit;

namespace GCaLink.Tests;

public sealed class OhioStateUniversityTests
{
    private readonly UniRegexService _service = new("Ohio State University");

    [Theory]
    [InlineData("Homework 1 [CSE 2231 AB1234]", "CSE 2231 AB1234")]
    [InlineData("Lab 2 [MATH 1151.01 CD5678]", "MATH 1151.01 CD5678")]
    [InlineData("Final Exam [PHYSICS 1250.02A EF9012]", "PHYSICS 1250.02A EF9012")]
    public void GetSectionInfo_ReturnsExpectedOutput(string input, string expected)
    {
        Assert.Equal(expected, _service.GetSectionInfo(input));
    }

    [Theory]
    [InlineData("Homework 1 [CSE 2231 AB1234]", "Homework 1")]
    [InlineData("Lab 2 [MATH 1151.01 CD5678]", "Lab 2")]
    [InlineData("Final Exam [PHYSICS 1250.02A EF9012]", "Final Exam")]
    public void GetAssignmentName_ReturnsExpectedOutput(string input, string expected)
    {
        Assert.Equal(expected, _service.GetAssignmentName(input));
    }

    [Theory]
    [InlineData("CSE 2231 AB1234", "CSE 2231")]
    [InlineData("MATH 1151.01 CD5678", "MATH 1151.01")]
    [InlineData("PHYSICS 1250.02A EF9012", "PHYSICS 1250.02A")]
    public void GetClassName_ReturnsExpectedOutput(string input, string expected)
    {
        Assert.Equal(expected, _service.GetClassName(input));
    }

    [Theory]
    [InlineData("CSE 2231 AB1234", "AB1234")]
    [InlineData("MATH 1151.01 CD5678", "CD5678")]
    [InlineData("PHYSICS 1250.02A EF9012", "EF9012")]
    public void GetSectionName_ReturnsExpectedOutput(string input, string expected)
    {
        Assert.Equal(expected, _service.GetSectionName(input));
    }
}