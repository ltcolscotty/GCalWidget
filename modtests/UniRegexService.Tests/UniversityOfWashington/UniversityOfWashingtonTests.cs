using MosaiCal.Services;
using Xunit;

namespace MosaiCal.Tests;

public sealed class UniversityOfWashingtonTests
{
    private readonly UniRegexService _service = new("University of Washington");

    [Theory]
    [InlineData("Lab A 123 D [BIOL 180 AA]", "BIOL 180 AA")]
    [InlineData("Final Exam [EDC&I 401 A]", "EDC&I 401 A")]
    [InlineData("Homework 1 [JSIS B 331 Q2]", "JSIS B 331 Q2")]
    [InlineData("Recitation [E E 215 A]", "E E 215 A")]
    [InlineData("Project Pt. 3 [LAW 600 A1]", "LAW 600 A1")]
    [InlineData("Quiz 10 [GEN ST 100 C]", "GEN ST 100 C")]
    public void GetSectionInfo_ReturnsExpectedOutput(string input, string expected)
    {
        Assert.Equal(expected, _service.GetSectionInfo(input));
    }

    [Theory]
    [InlineData("Lab A 123 D [BIOL 180 AA]", "Lab A 123 D")]
    [InlineData("Final Exam [EDC&I 401 A]", "Final Exam")]
    [InlineData("Homework 1 [JSIS B 331 Q2]", "Homework 1")]
    [InlineData("Recitation [E E 215 A]", "Recitation")]
    [InlineData("Project Pt. 3 [LAW 600 A1]", "Project Pt. 3")]
    [InlineData("Quiz 10 [GEN ST 100 C]", "Quiz 10")]
    public void GetAssignmentName_ReturnsExpectedOutput(string input, string expected)
    {
        Assert.Equal(expected, _service.GetAssignmentName(input));
    }

    [Theory]
    [InlineData("BIOL 180 AA", "BIOL 180")]
    [InlineData("EDC&I 401 A", "EDC&I 401")]
    [InlineData("JSIS B 331 Q2", "JSIS B 331")]
    [InlineData("E E 215 A", "E E 215")]
    [InlineData("LAW 600 A1", "LAW 600")]
    [InlineData("GEN ST 100 C", "GEN ST 100")]
    public void GetClassName_ReturnsExpectedOutput(string input, string expected)
    {
        Assert.Equal(expected, _service.GetClassName(input));
    }

    [Theory]
    [InlineData("BIOL 180 AA", "AA")]
    [InlineData("EDC&I 401 A", "A")]
    [InlineData("JSIS B 331 Q2", "Q2")]
    [InlineData("E E 215 A", "A")]
    [InlineData("LAW 600 A1", "A1")]
    [InlineData("GEN ST 100 C", "C")]
    public void GetSectionName_ReturnsExpectedOutput(string input, string expected)
    {
        Assert.Equal(expected, _service.GetSectionName(input));
    }
}