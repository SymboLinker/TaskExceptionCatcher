[![NuGet](https://img.shields.io/nuget/vpre/TaskExceptionCatcher.svg)](https://www.nuget.org/packages/TaskExceptionCatcher)

# TastExceptionCatcher

Use `Catcher.Run` for preventing a task to throw during a "one-fails-all" operation like `Task.WhenAll` or [AwaitMultiple](https://github.com/SymboLinker/AwaitMultiple).


## How to use and why

This example explains the syntax, but may not be useful beyond that:
```cs
var result = await Catcher.Run(() => ThrowOrNotAsync());
if (result.Exception != null)
{
    // this point will be reached in case of an exception.
}
else 
{
    var taskResultValue = result.Value.
}
```

When using [AwaitMultiple](https://www.nuget.org/packages/AwaitMultiple) or `Task.WhenAll`, an exception is thrown in case one task fails and you cannot access the values of the not-failed tasks.
In some cases, that may not be desired: you may want to continue if "getting value `b`" fails:
```cs
var (a, catchResultB, c) = await Tasks(
   StartTaskAAsync(),
   Catcher.Run(() => StartTaskBAsync()),
   StartTaskCAsync());
if (catchResultB.Exception is { } exception)
{
   // no problem!
}
else
{
   var b = catchResultB.Value;
   // use `b`.
}

// use `a` and `c`.
```

## Get it

Available via [NuGet](https://www.nuget.org/packages/TaskExceptionCatcher).
