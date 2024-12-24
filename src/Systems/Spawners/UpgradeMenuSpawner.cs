using System.Numerics;
using Ball;
using MoonTools.ECS;
using System.Collections;

public enum Upgrades
{
    Emergency, //+3 lives
    ChainReaction, //destroying blocks damages neighbors
    Buddy, // Extra paddle
    Bonus, //blocks can release ball when destroyed
    Refresh, //refresh upgrades once per chest
    Safety, // ball must hit bottom twice
    Piercing, //ball travels through destroyed blocks
    Confidence, //blocks spawn with less hp
    MedSchool, //extra lives give +2 
    Revenge, //losing a ball deals damage to all on-screen blocks
    OptimalHealth, //ball does 2x damage on 1 life remaining
    Invictus, //revive with one life, recharge with 3 lives
    Combo, //ball does +1 damage for each destroyed block until it returns to the paddle
}

public class UpgradeMenuSpawner : Manipulator
{

    static Queue<Upgrades> AvailableUpgrades = new Queue<Upgrades>();
    Filter DestroyFilter;
    MarqueeSpawner MarqueeSpawner;

    public static bool UpgradesAvailable()
    {
        return AvailableUpgrades.Count >= 3;
    }

    Upgrades SetUpgradeType(Entity e)
    {
        var upgrade = AvailableUpgrades.Dequeue();

        Set(e, new UpgradeOption(upgrade));
        return upgrade;
    }

    static string GetUpgradeName(Upgrades upgrade)
    {
        return upgrade switch
        {
            Upgrades.Emergency => "Revive",
            Upgrades.ChainReaction => "Chain",
            Upgrades.Buddy => "Buddy",
            Upgrades.Bonus => "Bonus",
            Upgrades.Refresh => "Refresh",
            Upgrades.Safety => "Safety",
            Upgrades.Piercing => "Piercing",
            Upgrades.Confidence => "Fright",
            Upgrades.MedSchool => "Medicine",
            Upgrades.Revenge => "Revenge",
            Upgrades.OptimalHealth => "Optimal",
            Upgrades.Invictus => "Invictus",
            Upgrades.Combo => "Combo",
            _ => throw new NotImplementedException()
        };
    }

    static string GetUpgradeDescription(Upgrades upgrade)
    {
        return upgrade switch
        {
            Upgrades.Emergency => "+3 Lives",
            Upgrades.ChainReaction => "Destroying blocks damages neighbors",
            Upgrades.Buddy => "Extra paddle",
            Upgrades.Bonus => "Blocks sometimes release extra balls",
            Upgrades.Refresh => "Get new upgrades",
            Upgrades.Safety => "Fragile barrier keeps the ball in play",
            Upgrades.Piercing => "Ball travels through destroyed blocks",
            Upgrades.Confidence => "Blocks spawn with less HP",
            Upgrades.MedSchool => "Extra lives give +2",
            Upgrades.Revenge => "Losing a life damages all blocks",
            Upgrades.OptimalHealth => "2x damage on 1 life remaining",
            Upgrades.Invictus => "Revive with one life, recharges at 3 lives",
            Upgrades.Combo => "+1 damage for each block destroyed without touching paddle",
            _ => throw new NotImplementedException()
        };
    }

    public static void ResetUpgrades()
    {
        AvailableUpgrades.Clear();
        foreach (var n in RandomManager.LinearCongruentialSequence(Enum.GetValues(typeof(Upgrades)).Length))
        {
            AvailableUpgrades.Enqueue((Upgrades)n);
        }
    }

    public UpgradeMenuSpawner(World world) : base(world)
    {
        DestroyFilter = FilterBuilder.Include<DestroyOnStateTransition>().Exclude<DontDestroyOnNextTransition>().Build();
        MarqueeSpawner = new MarqueeSpawner(world);
    }

    Entity CreateUpgrade(float x, MoonWorks.Graphics.Font.HorizontalAlignment horizontalAlignment)
    {
        var upgrade = CreateEntity();
        var type = SetUpgradeType(upgrade);
        Set(upgrade,
        new Position(new Vector2(x, Dimensions.GameHeight * 0.45f)));
        Set(upgrade,
         new Text(
            Fonts.HeaderFont,
            Fonts.UpgradeSize,
            Stores.TextStorage.GetID(GetUpgradeName(type)),
            horizontalAlignment,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(upgrade, new KeepOpacityWhenPaused());
        Set(upgrade, new Pause());
        Set(upgrade, new Depth(0.1f));
        Set(upgrade, new FollowsCamera(Dimensions.GameHeight * 0.45f));
        Set(upgrade, new DestroyOnStateTransition());

        var description = CreateEntity();
        Set(description,
            new Position(new Vector2(x, Dimensions.GameHeight * 0.5f)));
        Set(description,
         new Text(
            Fonts.BodyFont,
            Fonts.InfoSize,
            Stores.TextStorage.GetID(GetUpgradeDescription(type)),
            MoonWorks.Graphics.Font.HorizontalAlignment.Center,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(description, new KeepOpacityWhenPaused());
        Set(description, new Pause());
        Set(description, new Depth(0.1f));
        Set(description, new FollowsCamera(Dimensions.GameHeight * 0.5f));
        Set(description, new DestroyOnStateTransition());
        Set(description, new WordWrap(100));

        Relate(upgrade, description, new Description());
        Set(upgrade, new DestroyOnStateTransition());

        return upgrade;
    }

    public void OpenUpgradeMenu()
    {
        foreach (var entity in DestroyFilter.Entities)
        {
            Set(entity, new DontDestroyOnNextTransition());
        }

        var promptEntity = MarqueeSpawner.SpawnMarquee("UPGRADE", Fonts.HeaderFont, Fonts.HeaderSize, 2, 100f, Dimensions.GameHeight * 0.25f);
        Set(promptEntity, new Pause());

        var upgrade1 = CreateUpgrade(Dimensions.GameWidth * 0.15f, MoonWorks.Graphics.Font.HorizontalAlignment.Center);
        var upgrade2 = CreateUpgrade(Dimensions.GameWidth * 0.5f, MoonWorks.Graphics.Font.HorizontalAlignment.Center);
        var upgrade3 = CreateUpgrade(Dimensions.GameWidth * 0.85f, MoonWorks.Graphics.Font.HorizontalAlignment.Center);

        Relate(upgrade1, upgrade2, new HorizontalConnection());
        Relate(upgrade2, upgrade3, new HorizontalConnection());
        Relate(upgrade3, upgrade1, new HorizontalConnection());

        Set(upgrade2, new Selected());
    }

}