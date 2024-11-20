using System.Reflection.Metadata;
using MoonWorks.Graphics.Font;

namespace Ball;

public static class Dimensions
{
    public const int GameWidth = 480;
    public const int GameHeight = 480;
    public const float GameAspectRatio = GameWidth / (float)GameHeight;

    public const int WindowWidth = 1600;
    public const int WindowHeight = 900;
    public const float WindowAspectRatio = WindowWidth / (float)WindowHeight;
}

public static class Fonts
{
    public static int HeaderFont = Stores.FontStorage.GetID(Content.Fonts.F500Angular);
    public static int BodyFont = Stores.FontStorage.GetID(Content.Fonts.F5000);

    public const int HeaderSize = 52;
    public const int BodySize = 32;
}

public static class UILayoutConstants
{
    public const int LivesX = 150;
    public const int LivesY = 130;
    public const int LivesSpacing = 100;
    public const int InfoX = Dimensions.WindowWidth - 340;
    public const int ScoreLabelY = 60;
    public const int ScoreY = 90;
    public const int HighScoreLabelY = 170;
    public const int HighScoreY = 200;
    public const int GemsLabelY = 280;
    public const int GemsY = 310;
}