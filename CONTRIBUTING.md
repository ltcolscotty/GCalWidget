# Contributing to MosaiCal

Thanks for your interest in contributing to MosaiCal! MosaiCal is currently maintained by one developer, so review times may vary.

## Ways to contribute

- **University support:** Add or update Canvas event-name parsing configurations for your institution.
- **Bug reports and fixes:** Report reproducible problems or submit fixes.
- **Documentation:** Improve setup instructions, documentation, or examples.
- **Feature suggestions:** Suggest improvements that fit the project's scope.

Before starting larger features or architectural changes, check the [README](README.md), [Todo](Todo.md), existing issues, and open pull requests. Consider opening an issue first to discuss substantial changes.

## University support

See the [university regex documentation](docs/uniRegexService-docs.md#how-do-i-add-my-own-school) for instructions on adding or updating an institution.

When submitting changes, include the institution and sanitized examples demonstrating the expected behavior.

**Do not submit calendar feed URLs, authentication credentials, student information, private course information, or unredacted calendar data.** Treat Canvas ICS URLs as secrets.

If you aren't comfortable modifying the configuration yourself, feel free to open an issue with anonymized examples.

## Reporting bugs

When opening an issue, include:

- Steps to reproduce the problem.
- What you expected and what happened.
- Your MosaiCal and Windows versions.
- Relevant errors, screenshots, or logs with personal information removed.

Logs can be found under `%LocalAppData%\GCWidget\`, including `GCWLogs.txt` and similarly named files.

Please remove personal, course, and configuration information from logs unless necessary to reproduce the problem. Never post authentication tokens or ICS URLs.

## Pull requests

1. Fork the repository and create a branch for your change.
2. Keep changes focused and follow the existing code style.
3. Test affected behavior where practical.
4. Update relevant documentation when necessary.
5. Describe what changed, why, and how you tested it in your pull request.

For significant UI changes, include before/after screenshots when possible.

## Building locally

See [Building from source](README.md#building-from-source) for current requirements and dependencies.

## Project scope

MosaiCal focuses on providing a reliable and customizable Windows widget for Google Calendar and Canvas. Major features and integrations may be deferred to keep the project maintainable. Please discuss substantial additions before investing significant development time.