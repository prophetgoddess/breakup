using System.Reflection.Metadata;
using MoonWorks;
using MoonWorks.Graphics.Font;

namespace Ball;

public static class Dimensions
{
    public const int GameWidth = 480;
    public const int GameHeight = 480;
    public const float GameAspectRatio = GameWidth / (float)GameHeight;
    public static float GameAspectRatioReciprocal = 1f / GameAspectRatio;


    public const int UIWidth = 1600;
    public const int UIHeight = 900;
    public const float UIAspectRatio = UIWidth / (float)UIHeight;
    public const float UIAspectRatioReciprocal = 1f / UIAspectRatio;

}

public static class Fonts
{
    public static int HeaderFont = Stores.FontStorage.GetID(Content.Fonts.F500Angular);
    public static int BodyFont = Stores.FontStorage.GetID(Content.Fonts.F5000);

    public const int TitleSize = 72;
    public const int HeaderSize = 52;
    public const int MidSize = 42;
    public const int BodySize = 32;
    public const int PromptSize = 22;
    public const int UpgradeSize = 16;
    public const int InfoSize = 10;
}

public static class UILayoutConstants
{
    public const int TitleX = (int)(Dimensions.UIWidth * 0.5f);
    public const int TitleY = (int)(Dimensions.UIHeight * 0.45f);
    public const int PromptX = TitleX;
    public const int PromptY = (int)(Dimensions.UIHeight * 0.55f);
    public const int LivesX = 120;
    public const int LivesY = 130;
    public const int LivesSpacing = 100;
    public const int InfoX = Dimensions.UIWidth - 340;
    public const int ScoreLabelY = 60;
    public const int ScoreY = 90;
    public const int HighScoreLabelY = 170;
    public const int HighScoreY = 200;
    public const int GemsLabelY = 280;
    public const int GemsY = 310;
}

public static class GameplaySettings
{
    public static float MaxCameraY = 100;
}