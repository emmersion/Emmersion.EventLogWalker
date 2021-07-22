# Emmersion.EventLogWalker

Simplifies access to the Insights EventLog by providing a resumable `EventLogWalker` that exposes a single `InsightEvent` at a time.

## Configuration

In order to use this library, first call:

`Emmersion.EventLogWalker.Configuration.DependencyInjectionConfig.ConfigureServices(services);`

You must also provide and register implementations for: 

- `IInsightsSystemApiSettings`

## Usage

To get started, inject an `IEventLogWalker` into the class that will read the EventLog.

Example:

```c#
public SimplestReport(IEventLogWalker walker)
{
    this.walker = walker;
}
```

Then call `walker.WalkAsync(<WalkArgs>, <EventProcessor>)`.

Example `WalkArgs` (using defaults):

```c#
new WalkArgs()
```

Example `WalkArgs` (specifiying optional properties):

```c#
new WalkArgs
{
    StartInclusive = DateTimeOffset.Parse("2021-06-01"),
    EndExclusive = DateTimeOffset.Parse("2021-08-01")
}
```

The `EventProcessor` is a function which can be synchronous `Action<InsightEvent, IEventLogWalkerStatus>` or asynchronous `Func<InsightEvent, IEventLogWalkerStatus, Task>`.

Example call to `WalkAsync()`:

```c#
var finalStatus = await walker.WalkAsync(new WalkArgs(), (insightEvent, status) =>
{
    // your custom insightEvent processing code goes here
});
```

The result of calling `WalkAsync()` is an `IEventLogWalkerStatus`. To know if `WalkAsync()` completed successfully check `IEventLogWalkerStatus.Exception`. If it is `null` the walk finished successfully. If it is not `null` then an error occured while walking the EventLog.

Example detection of an error:

```c#
if (finalStatus.Exception != null)
{
    Console.WriteLine(finalStatus.Exception);
    return;
}
```

### API

#### `WalkArgs`
Properties:
- `StartInclusive`: The earliest (inclusive) InsightEvent to read. Default: `DateTimeOffset.MinValue`.
- `EndExclusive`: The latest (exlusive) InsightEvent to read. Default: `DateTimeOffset.MaxValue` 
- `ResumeToken`: The `string` returned by `IEventLogWalkerStatus.GetResumeToken()` from an earlier run.

#### `IEventLogWalkerStatus`
The object `IEventLogWalkerStatus` is provided to the `EventProcessor` function and returned by `WalkAsync()`. It contains information which can be used to make decisions about logging and persisting state for resuming.

Properties:
- `PageNumber`: The current page being processed or the last page processed when returned from `WalkAsync()`. (1 based)
- `PageEventIndex`: The index of the current event being processed or the last index attempted to be processed when returned from `WalkAsync()`.
- `PageEventsCount`: The count of events on the current page.
- `PageStatus` values:
    - `Empty`: The page contains no events.
    - `Start`: Processing the first event of the page.
    - `InProgress`: Processing an event of the page which is not the first or last event.
    - `End`: Processing the last event of the page.
    - `Done`: All events on the page have been processed.
- `TotalEventsProcessed`: The count of all events processed across all pages.
- `Exception`: Is not `null` when an error has occurred.

Methods:
- `string GetResumeToken()`: Returns a resume token which can be passed to `WalkArgs.ResumeToken` to resume walking at the current event.

### Resuming

Walking the EventLog can take hours to complete depending on the range of data specified in `WalkArgs`. Being able to resume after a failure or being able to stop and restart a walk can be very useful. Some things to keep in mind when enabling resuming:

- You get the resume token by calling `IEventLogWalkerStatus.GetResumeToken()`. Do not modify that token. Consider it a black box.
- When saving the resume token you also need to save the state of your custom processing.
- When saving state in the `EventProcessor` _only_ do so *before* processing that `InsightEvent`. If you persist after then when resuming it will process that `InsightEvent` a second time.
- Writing to disk can be expensive so we do not recommend saving state before processing each `InsightEvent`.
- We recommend saving state every so often as a checkpoint.
- We recommend always saving state when `status.Exception` is not `null`.

Example:

```c#
var resumeToken = LoadState(); // load custom state and pass the resume token to `WalkArgs.ResumeToken`

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