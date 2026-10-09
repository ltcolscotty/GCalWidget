# Win32Interop

Win32Interop's purpose is to interact with windows APIs for features like window transparency control, interprocess persistence to manage the calendar widget windows, and smooth shutdown among other things.

## Shutdown

The widget subclasses its native window to acknowledge `WM_QUERYENDSESSION` immediately, so it does not veto or delay Windows shutdown. It starts cleanup only after `WM_ENDSESSION` confirms shutdown. At that point, active source refreshes are canceled and the periodic refresh scheduler is stopped. The shutdown callback does not wait synchronously for network requests to finish.

## Hex Values

For anyone not familiar, the hex values set are bitfields, eg: `SWP_NOACTIVATE = 0x0010`is intended to be a bitfield of `0000 0000 0001 0000`. You can reference [PInvoke.net](https://pinvoke.net/) for the values (CsWin32 is also included in the csproj file).

## Microsoft References:

Direct links to microsoft interop documentation. If you're purely interested in what is primarily used in the [Win32Interop](..\src\GCaLink\GCaLink\Platform\Win32Interop.cs) code, [`System.Runtime.InteropServices`](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke) is the most docs reference to use. Otherwise, the full API docs are linked below.

[Full Win32 API](https://learn.microsoft.com/en-us/windows/win32/api/)