# University Test Template

Use this example when adding a test class for another supported university. Replace the placeholders with the university's registered name and concrete input/output values before enabling the test.

```csharp
using GCaLink.Services;
using Xunit;

namespace GCaLink.Tests;

public sealed class UniversityTests
{
    private readonly UniRegexService _service = new("<supported university name>");

    [Theory]
    [InlineData("<input>", "<expected output>")]
    public void Parser_ReturnsExpectedOutput(string input, string expected)
    {
        Assert.Equal(expected, _service.GetSectionInfo(input));
    }
}
```

The placeholders are illustrative only; this template does not define a supported university or an actual test case.