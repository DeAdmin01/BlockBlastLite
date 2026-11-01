namespace BlockBlastLite.Services;

public static class HapticService
{
    public static void Click()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Device does not support haptics
        }
    }

    public static void Heavy()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        }
        catch
        {
            // Device does not support haptics
        }
    }

    public static void Medium()
    {
        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(40));
        }
        catch
        {
            // Device does not support vibration
        }
    }

    public static void Vibrate(int milliseconds = 30)
    {
        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(milliseconds));
        }
        catch
        {
            // Device does not support vibration
        }
    }

    public static void Success()
    {
        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(70));
        }
        catch
        {
            // Device does not support vibration
        }
    }
}
