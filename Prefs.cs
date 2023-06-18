using Jevil.Prefs;

namespace SlideScale;

[Preferences(nameof(SlideScale), false)]
[PreferencesColor(UnityDefaultColor.GREEN)]
internal static class Prefs
{
    [RangePref(0, 0.125f, 0.0125f)]
    internal static float scaleMult = 0.025f;
    [RangePref(0, 0.5f, 0.025f)]
    internal static float deadzone = 0.1f; // ImSec*
    [RangePref(0, 10, 0.25f)]
    internal static float ghostSizeMult = 1;
    [Pref("Show 'ghosts' of your controllers, showing their real positions instead of where your ingame hands are")]
    internal static bool showGhosts = true;
    [Pref("Increaes object mass as you scale up an object and decreases it as you scale it down")]
    internal static bool scaleMass = true;
    [Pref("Toggles the mod on or off without requiring a complete uninstall")]
    internal static bool globalToggle = true;
    [Pref("Shows how big an object is when compared to its original size")]
    internal static bool showSizeText = true;
    [Pref("Does haptic rumble according to how big an object gets")]
    internal static bool useHaptics = true;
    [Pref("Attempts to find what to scale by looking for a root object. Slower and may fallback to default.")]
    internal static bool scaleUsingRoot = false;
    [Pref("", UnityDefaultColor.GRAY)]
    internal static bool minnesota = false;

    public static void Init()
    {
        Preferences.Register(typeof(Prefs));
    }
}