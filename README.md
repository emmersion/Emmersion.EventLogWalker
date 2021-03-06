# Emmersion.EventLogWalker

Event logs can contain millions of records making them unsuited for direct query access. This library aims to simplify reading event logs by providing a resumable event log walker which exposes a single item at a time. As of version 2.0 this libary exposes `IPager<TEvent>` which allows the `EventLogWalker` to target various event logs that can be walked by a `DateTimeOffset`.

This has been [open sourced](https://github.com/emmersion/engineering-at-emmersion#open-source)
under the [MIT License](./LICENSE). The concepts in this code may be useful for others who are wanting to deal with large volumes of data while staying in the .NET / C# stack.


## Configuration

In order to use this library, first call:

`Emmersion.EventLogWalker.Configuration.DependencyInjectionConfig.ConfigureServices(services);`

### For Emmersion Insights Event Log

You must also provide and register implementations for: 

- `IInsightsSystemApiSettings`

*NOTE:* In order to obtain the required Insights System Api key you will need to reach out to the Strategic Solutions Team.

### For Custom Event Logs

You must also provide and register implementations for: 

- `IPager<TEvent>`

## Getting started

This documentation will refer to consumers of `IEventLogWalker` as reports. To create a report, inject an `IEventLogWalker`.

Example:

```c#
private readonly IEventLogWalker walker;

public ExampleReport(IEventLogWalker walker, IPager<InsightEvent>)
{
    this.walker = walker;
}
```

Call either:
- `Task<IEventLogWalkerStatus> WalkAsync<TEvent>(IPager<TEvent> pager, WalkArgs args, Func<TEvent, IEventLogWalkerStatus, Task> eventProcessor)`
- `Task<IEventLogWalkerStatus> WalkAsync<TEvent>(IPager<TEvent> pager, WalkArgs args, Action<TEvent, IEventLogWalkerStatus> eventProcessor)` 

Example call to `WalkAsync()` with default `args` and a synchronous `eventProcessor`:

```c#
private readonly IPager<InsightEvent> pager;

...

var finalStatus = await walker.WalkAsync(pager: insightsPager, args: new WalkArgs(), eventProcessor: (insightEvent, status) =>
{
    // your custom insightEvent processing code goes here
});
```

*NOTE:* Replace `IPager<InsightEvent>` with your custom `IPager<TEvent>` implementation.

The returned `IEventLogWalkerStatus.Exception` can be checked to determine whether `WalkAsync()` reached the end the event log for the range specified by `WalkArgs` or if an error occured. If `Exception` is `null` the walk finished successfully. If it is not `null` then an error occured.

Example detection of an error:

```c#
if (finalStatus.Exception != null)
{
    Console.WriteLine(finalStatus.Exception);
    return;
}
```

## API

### `IEventLogWalker`
Methods:
- `Task<IEventLogWalkerStatus> WalkAsync<TEvent>(IPager<TEvent> pager, WalkArgs args, Action<TEvent, IEventLogWalkerStatus> eventProcessor)`: Use this overload, which takes an `Action`, to process each `InsightEvent` synchronously, like when all state is stored only in memory.
- `Task<IEventLogWalkerStatus> WalkAsync<TEvent>(IPager<TEvent> pager, WalkArgs args, Func<TEvent, IEventLogWalkerStatus, Task> eventProcessor)`: Use this overload, which takes a `Func`, to process each `InsightEvent` asynchronously, like when writing to disk or a database.

### `WalkArgs`
Properties:
- `StartInclusive`: The earliest (inclusive) InsightEvent to read. Default: `DateTimeOffset.MinValue`.
- `EndExclusive`: The latest (exlusive) InsightEvent to read. Default: `DateTimeOffset.MaxValue` 
- `ResumeToken`: When provided the walker will resume processing instead of beginning at the `StartInclusive` date/time.

### `IEventLogWalkerStatus`
Contains information which can be used to make decisions about logging and persisting state for resuming.

Properties:
- `PageNumber`: The page being processed. One (1) based counter.
- `PageEventIndex`: The index of the event being processed.
- `PageEventsCount`: The count of events on the page.
- `PageStatus` (enum values):
    - `Empty`: The page contains no events.
    - `Start`: Processing the first event of the page.
    - `InProgress`: Processing an event of the page which is not the first or last event.
    - `End`: Processing the last event of the page.
    - `Done`: All events on the page have been processed.
- `TotalEventsProcessed`: The count of all events processed across all pages.
- `Exception`: Is not `null` when an error has occurred.

Methods:
- `string GetResumeToken()`: Returns a `resumeToken` which can be passed to `WalkArgs.ResumeToken` to resume walking at the `PageEventIndex`.

### `InsightEvent`
Properties:
- `Id`: Unique Id in the Insights product context, useful when troubleshooting issues with the Insights team.
- `BrowserTimestamp`: When the browser thought the event occured.
- `ServerTimestamp`: When the server delared the event occured. Use this as the true time of occurance.
- `UserId`: Which user triggered the event.
- `AccountId`: Which account the user belongs to.
- `AuthSession`: Which session the user triggered the event during.
- `EventType`: Name of the triggered event.
- `Data`: JSON payload containing more information about the event.


## Resuming

Walking the EventLog can take hours to complete depending on the range of data specified in `WalkArgs`. Being able to resume after a failure or being able to stop and restart a walk can be very useful. Some things to keep in mind when enabling resuming:

- You get the `resumeToken` by calling `IEventLogWalkerStatus.GetResumeToken()`. Do not modify that token. Consider it a black box.
- When saving the `resumeToken` you also need to save the state of your report.
- When saving state in the `eventProcessor` _only_ do so *before* processing the `insightEvent`. If you persist after then when resuming it will process that `InsightEvent` a second time.
- Writing to disk can be expensive so we do not recommend saving state before processing each `InsightEvent`.
- We recommend saving state every so often as a checkpoint. For example, at the start of each page when `status.PageStatus == PageStatus.Start`.
- We recommend always saving state when the `status` returned by `WalkAsync()` has a non-`null` value for `status.Exception`.

Example:

```c#
var resumeToken = LoadState(); // load custom state and pass the `resumeToken` to `WalkArgs.ResumeToken`

var finalStatus = await walker.WalkAsync(new WalkArgs{ resumeToken = resumeToken}, (insightEvent, status) =>
{
    if (status.PageStatus == PageStatus.Start)
    {
        SaveState(status.GetResumeToken()); // only before processing insightEvent
    }
    
    // process insightEvent here only after persisting state
});

if (finalStatus.Exception != null)
{
    SaveState(status.GetResumeToken());
    return;
}
```

## Example Reports
The included `ExampleReports` project has a few examples of consuming `IEventLogWalker`. You can view them in GitHub here:
- [SimpleReport](https://github.com/emmersion/Emmersion.EventLogWalker/blob/main/ExampleReports/SimpleReport.cs)
  - Bare minimum to get up and running.
- [ResumeValidationReport](https://github.com/emmersion/Emmersion.EventLogWalker/blob/main/ExampleReports/ResumeValidationReport.cs)
  - Bare minimum to get up and running.
- [AccountUserCountsReport](https://github.com/emmersion/Emmersion.EventLogWalker/blob/main/ExampleReports/AccountUserCountsReport.cs)
  - Supports resuming
  - Supports custom date ranges
  - Great as a template to copy and paste in order to get started.


## Version History

2.0
- Expose `IPager<TEvent>` to allow various custom event log sources.
    - The library is no longer tightly coupled to Emmerion's internal Insights product context event log. However, at this time the Insights event log `IPager<InsightEvent>` is still included in this library. Though it's not marked, it should be treated as obsolete.