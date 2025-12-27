using BepInEx.Configuration;

namespace IronDeficiency;

public class Config {
    public static Config Instance { get; private set; } = null!;

    // Tripping
    public ConfigEntry<bool> EnableRandomTrips { get; private set; }
    public ConfigEntry<float> TripChancePerSecond { get; private set; }
    public ConfigEntry<float> MinMovementSpeed { get; private set; }

    // Fainting
    public ConfigEntry<bool> EnableRandomFaints { get; private set; }
    public ConfigEntry<float> FaintChancePerSecond { get; private set; }
    public ConfigEntry<bool> CanFaintWhileClimbing { get; private set; }

    public Config(ConfigFile config) {
        Instance = this;

        // Tripping
        EnableRandomTrips = config.Bind("Tripping", "EnableRandomTrips", true, "Enable random tripping.");
        TripChancePerSecond = config.Bind("Tripping", "TripChancePerSecond", 0.02f, "Chance per second to trip (0.01 = 1% per second)");
        MinMovementSpeed = config.Bind("Tripping", "MinMovementSpeed", 1.5f, "Minimum movement speed to be able to trip.");

        // Fainting
        EnableRandomFaints = config.Bind("Fainting", "EnableRandomFaints", true, "Enable random fainting.");
        FaintChancePerSecond = config.Bind("Fainting", "FaintChancePerSecond", 0.006f, "Chance per second to faint (0.005 = 0.5% per second)");
        CanFaintWhileClimbing = config.Bind("Fainting", "CanFaintWhileClimbing", true, "Whether you can faint while climbing.");
    }
}