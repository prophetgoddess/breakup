using MoonWorks.Graphics.Font;

namespace Ball;

public struct Text
{
    public int FontID { get; }
    public int Size { get; }
    public int TextID { get; }
    public HorizontalAlignment HorizontalAlignment { get; }
    public VerticalAlignment VerticalAlignment { get; }

    public Text(
        int fontID,
        int size,
        string text,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment verticalAlignment = VerticalAlignment.Baseline
    )
    {
        FontID = fontID;
        Size = size;
        TextID = Stores.TextStorage.GetID(text);
        HorizontalAlignment = horizontalAlignment;
        VerticalAlignment = verticalAlignment;
    }

    public Text(
        int fontID,
        int size,
        int textID,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment verticalAlignment = VerticalAlignment.Baseline
    )
    {
        FontID = fontID;
        Size = size;
        TextID = textID;
        HorizontalAlignment = horizontalAlignment;
        VerticalAlignment = verticalAlignment;
    }
}
