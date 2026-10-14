### V1 Release Targets

Target: Winter 2027

#### Core Functionality

- [x] Automatic refreshing
- [x] On startup request
- [x] Prevent processes from interrupting shutdown
- [ ] Disable specific sources
- [x] Default fallback case for unsupported universities in parsing handler
- [ ] Bracket edge case in assignment name parsing
  - [ ] Tighten up regexes

#### Customization

- [ ] Solid color customization option for event backgrounds
- [ ] Color selector UI
- [ ] Good looking default config
- [ ] Darkness adjuster for images per source

#### Reliability

- [ ] Race condition on startup for open on start dialouge
- [ ] Prevent multiple controllers from opening
- [ ] Basic checks for memory leaks
- [ ] Test extended runtime
- [ ] Test sleep / wake behavior
- [ ] Test network loss and recovery
- [ ] Test configuration persistence
- [ ] Test Google authentication failure / expiration
- [ ] Test malformed or unavailable Canvas ICS feeds

#### Distribution

- [ ] Signed installer
- [ ] Test clean installation on another Windows machine
- [ ] Test uninstall
- [ ] Decide initial distribution method
- [ ] Finalize Google OAuth production configuration

#### Documentation & Release

- [ ] Finish essential docs
- [ ] Finish README for V1
  - [ ] Installation instructions
  - [ ] Setup instructions
  - [ ] Customization documentation
  - [ ] Screenshots
  - [ ] Short setup / demonstration GIF
- [ ] Finalize CONTRIBUTING.md
- [ ] Prepare V1 release notes

---

### Todo

#### In Consideration

- [ ] Font customization
- [ ] Source title color customization
- [ ] Apple iCal support
- [ ] Microsoft Outlook support
- [ ] Multiple Google account support

#### \<font color="lightgreen"> High Priority \</font>

- [ ] Accent Bar for lighter customization options
- [ ] Solid/Mica/Acrylic customization options for calendar view window
- [ ] Independent transparency customization for view background
- [ ] Separation of google calendar sources by calendar color

#### \<font color="green"> Low Priority \</font>

- [ ] Gif support for events
- [ ] Controller window background image customization
- [ ] Memory usage optimization
  - [ ] Benchmark view usage
- [ ] Real documentation inside the code
- [ ] Liquid Glass background

### In Progress

- [ ] Signed Installer

### Done

- [x] Prevent processes from interrupting shutdown
- [x] Automatic Refreshing
- [x] On startup request

### Conventions

Tasks moved to [Done](#done) should be removed somewhat shortly after being finished to reduce clutter.

**V1 Release Targets** are tasks required before the initial public release. New features should generally not be added to the V1 target unless they are necessary for normal use, reliability, security, or distribution.

**High priority** — Essential to a planned feature or core functionality, or a critical bug.

**Low priority** — Minor QoL changes or improvements that are not required for normal operation.

**In Consideration** — Potential future features that are not currently committed to a release.

#### Copy/Paste References

- [ ] Not done
- [x] Done

#### Reference

[Todo.md](https://github.com/todomd/todo.md)
