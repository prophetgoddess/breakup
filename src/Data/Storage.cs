using MoonWorks.Audio;
using MoonWorks.Graphics.Font;

namespace Ball;

public static class Stores
{
    public static Storage<string> TextStorage = new Storage<string>();
    public static Storage<Font> FontStorage = new Storage<Font>();
    public static Storage<AudioBuffer> SFXStorage = new Storage<AudioBuffer>();
}

// Generic class for storing managed types
public class Storage<T>
{
    Dictionary<T, int> ToID = new Dictionary<T, int>();
    T[] IDTo = new T[256];
    Stack<int> OpenIDs = new Stack<int>();
    int NextID = 0;

    public T Get(int id)
    {
        return IDTo[id];
    }

    public int GetID(T text)
    {
        if (!ToID.ContainsKey(text))
        {
            Register(text);
        }

        return ToID[text];
    }

    private void Register(T text)
    {
        if (OpenIDs.Count == 0)
        {
            if (NextID >= IDTo.Length)
            {
                System.Array.Resize(ref IDTo, IDTo.Length * 2);
            }
            ToID[text] = NextID;
            IDTo[NextID] = text;
            NextID += 1;
        }
        else
        {
            ToID[text] = OpenIDs.Pop();
        }
    }
}

