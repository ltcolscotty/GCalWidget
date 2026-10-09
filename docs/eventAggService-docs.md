# EventAggService

## Purpose

Centralized event aggregator class. This should be the only part of GCaLink to modify the `GCWMainData.msgpack` file. Support for other calendar providers should be centralized to [EventAggService.cs](..\src\GCaLink\GCaLink\Services\EventAggService.cs) after making a dedicated service to call the API and parse the return into [CalEventDto](..\src\GCaLink\GCaLink\Models\CalEventDto.cs) objects.

## Read/Write - Locks (IMPORTANT)

To ensure data races don't happen, use the `_calEventMsgPackLock` lock to ensure atomicity. 

## Aggregation

There are individual `Refresh<source>()` functions as well as the `RefreshAllAsync()` function to call updates and refresh data from a specific and all sources as their namesake suggests. Any new sources should continue to follow this convention where there is a dedicated refresher function and a call to it in `RefreshAllAsync()`.

