
namespace JANOARG.Chartmaker.Utils.NativeAPI
{

    public enum CursorStyle
    {
        None = -1,

        Arrow        = 0,
        Crosshair    = 1,
        Text         = 100,
        TextVertical = 101,

        Busy           = 1000,
        BackgroundBusy = 1001,
        Blocked        = 1100,

        HandPointing        = 2000,
        HandGrabReady       = 2100,
        HandGrabbing        = 2200,
        HandGrabbingBlocked = 2201,

        ResizeLeft             = 3000,
        ResizeRight            = 3001,
        ResizeTop              = 3002,
        ResizeBottom           = 3003,
        ResizeTopLeft          = 3004,
        ResizeTopRight         = 3005,
        ResizeBottomLeft       = 3006,
        ResizeBottomRight      = 3007,
        ResizeVertical         = 3008,
        ResizeHorizontal       = 3009,
        ResizeDiagonalBottomLeftTopRight = 3010,
        ResizeDiagonalTopLeftBottomRight = 3011,
    }

    /// <summary>
    /// The preferred cursor display style.
    /// </summary>
    public enum PreferredCursorMode
    {
        /// <summary>
        /// Prioritizes app-defined cursors, falls back to OS-defined cursors when no best fit is available.
        /// </summary>
        PreferCustom,

        /// <summary>
        /// Prioritizes OS-defined cursors, falls back to app-defined cursors when no best fit is available.
        /// </summary>
        PreferNative,

        /// <summary>
        /// Prioritizes OS-defined cursors, falls back to other OS-defined cursors when no best fit is available.
        /// </summary>
        PreferNativeBestEffort,
    }
}
