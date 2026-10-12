# MosaiCal

(i need to make a logo and get some screenshots for this lol)

MosaiCal is a customizable Windows desktop calendar widget that aggregates Google Calendar and Canvas events together in one place with per-source customization controls.

Version: In Development

## Features
- Google Calendar integration
- Canvas calendar integration through ICS
- Automatic Canvas assignment-name cleanup
- Per-course event customization
- Mica and Acrylic backgrounds
- Adjustable widget transparency
- Local data storage
- Automatic calendar synchronization

## Installation

Coming soon

## Setup
### Google Calendar

1. Sign in with Google via OAuth
2. Enable google as a tracked source in the main settings page
3. Refresh and you're set!

### Canvas

1. Go to your calendar on Canvas
2. Click the 'Calendar Feed' button at the right
3. Copy the provided link into MosaiCal's Canvas tab
4. Enable Canvas as a tracked source in the main settings page
5. Refresh and you're set!

TODO: Short tutorial gif

## Customization

Description coming soon

## University Support
See [contributing](#contributing) for information on adding university configurations

### Supported List:
MosaiCal can use Canvas calendar feeds from other universities, but university-specific configurations improve assignment name cleanup and course detection. The following universities have tested configurations:
- Arizona State University
- Ohio State University
- University of Washington

## Limitations
- Currently available only for Windows
- University-specific Canvas event formatting may require a supported configuration
- Currently tested against Arizona State, Ohio State, and University of Washington
- Only supports one Google Calendar account

## Privacy
MosaiCal does not have an external server to upload calendar data, nor does it have telemetry. Calendar and configuration data is stored locally on your device. Network connections are only made when authenticating with supported services and retrieving data during syncing.

## Contributing
Contributions are welcome, in particular:
- Adding or updating university canvas configurations
- Bug fixes and compatibility improvements
- Documentation improvements
- New features

To add support for a new university, see [uniRegexService-docs.md](docs/uniRegexService-docs.md#how-do-i-add-my-own-school).

See [CONTRIBUTING.md](CONTRIBUTING.md) for general contribution guidelines.

## Building from source
### Requirements
- Windows 11
- .NET10 SDK
- Windows App SDK

### Dependencies
- Google APIs
- MessagePack
- ICal.Net

## Documentation
Find the docs in the `docs/` folder. Helpful for anyone wanting to contribute, or just curious about how everything works.