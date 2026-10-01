# LoggerService

## Purpose

`LoggerService.cs` is intended to be a useful debugging helper for dealing with crashes and issues. Providing logs in a `.txt` file should help with diagnosing if certain actions are recieved and errors or warnings that may have been hidden that led up to an issue.

## How it works

### Log Location

The default log location is in `%LOCALAPPDATA%\GCWidget\GCWLogs.txt` for all builds.

### Log limits

In order to prevent logs from taking up a tremendous amount of space, log files are limited to `1 MiB` in size, and there are three such files allowed to exist at one time, older files will automatically be discarded. The size and number of files is configurable in a set constant if larger files or more files are needed. If you want to save a particular log you can always just move it to another location for reference for future debugging.

### Enums

Use the enum `LoggerStatusEnum` when specifying information to log this includes the following:

- `LoggerStatusEnum.INFO` - General information that could be helpful in tracing things like user actions, successful responses, etc.
- `LoggerStatusEnum.WARNING` - Low severity issues, the code can easily handle and recover from it
- `LoggerStatusEnum.EXCEPTION` - Exception thrown by the some runtime issue. Recoverable or irrecoverable, medium to high severity. A specific `LogException()` function exists for this, though technically speaking, the standard `Log()` function can be used if some custom formatting is desired. 
- `LoggerStatusEnum.ERROR` - High severity issue that caused a crash. Generally no handling is made for it.

## Conventions

`LoggerService.Log()` handles the absolute essentials including the text itself, the timestamp, the level of severity, and PID. Otherwise, log text should contain the source of the log to help more directly trace the information being logged.

An ideal log message contains the module that is making the log, as well as the actual information including attempted actions, and if possible direct dump of what happened to cause the issue if an exception was made.

