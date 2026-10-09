# EventAggService

## Purpose

Centralized event aggregator class. This should be the only part of GCaLink to modify the `GCWMainData.msgpack` file. Support for other calendar providers should be centralized to [EventAggService.cs](..\src\GCaLink\GCaLink\Services\EventAggService.cs) after making a dedicated service to call the API and parse the return into [CalEventDto](..\src\GCaLink\GCaLink\Models\CalEventDto.cs) objects.

## Read/Write - Locks (IMPORTANT)

All access to the calendar message-pack file should go through `AcquireCalendarDataLockAsync()`. That method creates/opens a sidecar lock file at `SettingsRetriever.GetMainDataPath() + ".lock"` using `FileStream` with `FileShare.None`, which serializes readers and writers and prevents concurrent reads/writes to `GCWMainData.msgpack`.

This is the actual data-safety lock for the message pack file; the `eventsChangedLock` is only for debounced view notifications and not for protecting the calendar data itself.

## Aggregation

There are individual `Refresh<source>()` functions as well as the `RefreshAllAsync()` function to call updates and refresh data from a specific and all sources as their namesake suggests. Any new sources should continue to follow this convention where there is a dedicated refresher function and a call to it in `RefreshAllAsync()`.

