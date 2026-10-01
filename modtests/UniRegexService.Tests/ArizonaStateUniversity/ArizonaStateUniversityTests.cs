using GCaLink.Services;
using Xunit;

namespace GCaLink.Tests;

public sealed class ArizonaStateUniversityTests
{
    private readonly UniRegexService _service = new("Arizona State University");

    [Theory]
    [InlineData("Homework 1 [CSE101 1234A]", "CSE101 1234A")]
    [InlineData("Exam 2 [MAT265 5678B]", "MAT265 5678B")]
    [InlineData("Final Project [SER316 9999C]", "SER316 9999C")]
    public void GetSectionInfo_ReturnsExpectedOutput(string input, string expected)
    {
        Assert.Equal(expected, _service.GetSectionInfo(input));
    }

    [Theory]
    [InlineData("Homework 1 [CSE101 1234A]", "Homework 1")]
    [InlineData("Exam 2 [MAT265 5678B]", "Exam 2")]
    [InlineData("Final Project [SER316 9999C]", "Final Project")]
    public void GetAssignmentName_ReturnsExpectedOutput(string input, string expected)
    {
        Assert.Equal(expected, _service.GetAssignmentName(input));
    }

    [Theory]
    [InlineData("CSE101 1234A", "CSE101")]
    [InlineData("MAT265 5678B", "MAT265")]
    [InlineData("SER316 9999C", "SER316")]
    public void GetClassName_ReturnsExpectedOutput(string input, string expected)
    {
        Assert.Equal(expected, _service.GetClassName(input));
    }

    [Theory]
    [InlineData("CSE101 1234A", "1234A")]
    [InlineData("MAT265 5678B", "5678B")]
    [InlineData("SER316 9999C", "9999C")]
    public void GetSectionName_ReturnsExpectedOutput(string input, string expected)
    {
        Assert.Equal(expected, _service.GetSectionName(input));
    }
}