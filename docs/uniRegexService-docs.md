# UniRegexService

## Purpose

`UniRegexService.cs` aggregates the regular expressions used to parse Canvas event titles and identify course and section information for by-class customization.

Each university configuration must define the following functions:

- `SectionInfo` - Extracts the section/course information from the assignment summary.
- `AssignmentName` - Extracts the assignment name from the assignment summary.
- `ClassName` - Extracts the course/class identifier from `SectionInfo`.
- `SectionName` - Extracts the section identifier from `SectionInfo`.

## Parsing Flow

Parsing occurs in two stages:

```text
Canvas Event Summary
├── AssignmentName
└── SectionInfo
    ├── ClassName
    └── SectionName
```

`SectionInfo` and `AssignmentName` receive the original Canvas event summary.

`ClassName` and `SectionName` receive the value returned by `SectionInfo`.

For example:

```text
"Homework 1 [CSE 101 AB1234]"
    │
    ├── AssignmentName → "Homework 1"
    │
    └── SectionInfo → "CSE 101 AB1234"
                        │
                        ├── ClassName   → "CSE 101"
                        └── SectionName → "AB1234"
```

## How Do I Add My School?

### Default Parser
If the university does not have an entry in `_uniParsers`, `UniRegexService` uses `_defaultParser`. The default parser provides generalized parsing behavior for universities that follow the standard Canvas event-title format. University-specific parsers should only be added when a university's naming convention cannot be handled correctly by the default parser.

### Adding a Parser
Before adding a new regular expression, check whether one of the existing universal or generalized definitions supports your school's naming format.

If a university requires its own parsing rules:

1. Add university-specific parsing functions for the definitions that differ from the existing ones.
2. Keep each definition as a `Regex.Match`, even if its current behavior could be implemented another way. University naming conventions may contain edge cases that require the expression to diverge later.
3. Create and add a `UniParser` object for the university to the table `_uniParsers`.
4. Define all four required functions `SectionInfo`, `AssignmentName`, `ClassName`, and `SectionName`.

### Parser Format
A university configuration should follow this structure:

```csharp
["University Name"] = new UniParser
{
    SectionInfo = SectionInfoUniversal,
    AssignmentName = AssignmentNameUniversal,
    ClassName = GetUniversityCN,
    SectionName = GetUniversitySN
}
```

Use `MatchOrInput()` for ClassName parser lambdas. If the regex does not match, preserving the input provides a usable class identifier rather than an empty value. Class names are used to distinguish customization groups and are displayed to users.
Use `MatchOrEmpty()` for SectionName parser lambdas. A section is supplemental information and is not required to distinguish classes, so a failed section match should return an empty string rather than duplicate the input.

## Testing

The xUnit test project is located at `modtests/UniRegexService.Tests`. Run it from the repository root with:

```powershell
dotnet test modtests/UniRegexService.Tests/UniRegexService.Tests.csproj
```

Tests are organized into respective university folders. Each fixture creates its own `UniRegexService` with a registered university name; the universal fixture uses Arizona State University's configuration because the universal parsers are shared by each university configuration. These folders currently contain setup only and no behavior-specific test cases.

Use the example in `Template/README.md` as a starting point when adding a test class and concrete cases for another university.