# Widget Window Interprocess Persistence

## Purpose

GCaLink can have more than one `gcalink.exe` process at a time. The widget controller owns its settings window, while a calendar view may have been created by an earlier process and remain alive after that controller closes. Each process has its own `ViewWindowManager` and in-memory settings; shared files and Win32 window operations coordinate them.

The controller closing is not intended to close the primary calendar view. It publishes that manager interaction is inactive, allowing the existing view to return to its desktop-widget style while its owning process keeps the window alive.

## Ownership Model

A primary view (`Todo`, `Day`, or `Week`) is created and retained by the process that first creates it. That process owns the WinUI `Window`, its dispatcher, event subscriptions, and its `primaryViewType`. A later controller process usually has no local primary view. It can locate an existing top-level window by its title, but it cannot directly call methods on the other process's WinUI object.

`ViewWindowManager` therefore uses two complementary mechanisms:

1. Win32 interop changes the native style and activation behavior of a window found by title.
2. Shared state files notify the owning process when manager activity or the selected primary view changes.

## Shared State Files

Both files are stored beside the calendar data in `%LOCALAPPDATA%\GCWidget`.

### `GCWWidgetManagerState`

This file contains a Boolean (`True` or `False`) representing whether the widget controller is active.

- `WidgetWindow` calls `SetPrimaryViewManagerActive(true)` when opened or activated, and `false` when closed.
- A process publishes only when its local active value changes.
- Publishing writes a process-specific temporary file, then replaces `GCWWidgetManagerState` with a move operation.
- Each process watches the state file for changes, creation, and rename. Events are debounced for 150 ms before the latest value is read.
- A receiving process marshals native window-mode updates to the primary view's `DispatcherQueue`.
- An active update also reloads persisted calendar customizations in the view-owning process.

This message changes interaction mode; it does not choose `Todo`, `Day`, or `Week` and does not close the primary view.

### `GCWPrimaryViewState`

This file contains JSON with the controller process ID, whether the primary view is enabled, and the selected `PrimaryViewEnum` value.

- `SyncWithSettings` publishes the current selection and enabled state.
- Each process watches this file with the same 150 ms debounce.
- A process ignores messages written by its own process ID.
- The owning process marshals the update to its dispatcher, updates its local settings, then creates, replaces, or closes its local primary view.
- When replacing a view it owns, the process closes the current window and creates the requested view directly. It does not search for the just-closed window by title.

The state file is a latest-value message, not a queue. If multiple selections happen quickly, the receiver reads the most recently written state after debounce.

## Native Window Modes

`DesktopWidgetWindowBehavior` applies the WinUI presenter settings, then `Win32Interop.SetDesktopWidgetMode` updates native styles using `GetWindowLongPtr` / `SetWindowLongPtr` and calls `SetWindowPos`.

- **Controller active:** remove `WS_EX_TOOLWINDOW` and `WS_EX_NOACTIVATE`, restore the caption and sizing frame, and place the window at the top of the non-topmost z-order so it can be manipulated.
- **Controller inactive:** add tool-window and no-activate styles, remove the caption and sizing frame, and place the window at `HWND_NOTOPMOST`. The window remains alive and is not deliberately sent behind the desktop shell.
- Both transitions preserve the current position and size by passing `SWP_NOMOVE` and `SWP_NOSIZE`.

The interop layer finds existing windows by their exact titles: `GCaLink Day View`, `GCaLink Week View`, and `GCaLink Todo View`. The pinned view is similarly activated by its title. `FindWindow` returns a native handle; it does not establish ownership of the remote WinUI window.

## Position and Size Persistence

Position and size are separate from process coordination. `WindowConfiguration.Configure` restores the primary or pinned view's saved size and position from `SettingsRetriever`. `AppWindow.Changed` persists changes, and the window's `Closed` event saves the final state.

The values are stored in `GCWConfig.json` under `%LOCALAPPDATA%\GCWidget`. Closing the controller does not itself save or reset the calendar view's position; closing or moving/resizing the view does.

## Customization Refresh

Source-event configuration is loaded from `ETCSettings.msgpack`; source image associations are loaded from `SourceImages\SourceImageAssociations.json`. These are process-local caches backed by shared files.

- `SettingsRetriever` watches `ETCSettings.msgpack` and reloads the config when it changes.
- `SourceImageService` watches the image-association JSON and reloads its association map when it changes.
- The manager-state activation path explicitly reloads both caches in the process that owns the primary view.
- `SourceImagesChanged` and `EventAggService.EventsChanged` cause Todo, Day, and Week to rebuild their displayed events and brushes.

An image association is keyed by provider and source ID (for example, `Canvas:CSE340`). A loaded association alone does not guarantee an event will use it; the event's computed identity must match the association key.

## Diagnostics

`GCWLogs.txt` is also under `%LOCALAPPDATA%\GCWidget`. Each line includes a process ID so entries from the controller and view-owning process can be distinguished.

Useful entries when investigating a reopen or view swap include:

- `Published manager state` and `Received manager state`
- `Published primary view state` and `Applying primary view state`
- `Primary view state update enqueued` and `Applying manager mode`
- `Close primary requested` and `Primary calendar view Closed event received`
- `Applied widget manager mode ... positioned=...`
- `SourceImageService: Reloaded ... image associations`
- `CalendarEventDisplayService: ... identity=... imageMatched=...`
- `TodoView`, `DayView`, or `WeekView` refresh and rebuilt-event messages
- `Failed applying primary view state` or `Unhandled application exception` with exception details

A manager-state update without a primary-view-state update can change native interaction mode but cannot switch the selected calendar view. A `Closed` event without a preceding manager-requested close may indicate a user close or another window-lifecycle path.
