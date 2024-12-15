using System.Collections;
using Ball;
using MoonTools.ECS;
using MoonWorks.Audio;

public class Audio : MoonTools.ECS.System
{
    Filter SFXFilter;
    AudioDevice AudioDevice;

    StreamingVoice MusicVoice;

    Queue<PersistentVoice> Voices = new Queue<PersistentVoice>();
    Queue<PersistentVoice> Playing = new Queue<PersistentVoice>();

    PersistentVoice GetVoice()
    {
        if (Voices.Count > 0)
        {
            return Voices.Dequeue();
        }
        return new PersistentVoice(AudioDevice, Content.SFX.pop.Format);
    }

    public Audio(World world, AudioDevice audioDevice) : base(world)
    {
        AudioDevice = audioDevice;
        SFXFilter = FilterBuilder.Include<PlayOnce>().Build();
        MusicVoice = new StreamingVoice(audioDevice, Content.Music.music.Format);
        Content.Music.music.Load();
        MusicVoice.Loop = true;
        MusicVoice.SetVolume(0.5f);
        MusicVoice.Load(Content.Music.music);
        MusicVoice.Play();
    }

    public override void Update(TimeSpan delta)
    {
        while (Playing.Count > 0 && Playing.Peek().State != SoundState.Playing)
        {
            Voices.Enqueue(Playing.Dequeue());
        }

        foreach (var entity in SFXFilter.Entities)
        {
            var voice = GetVoice();
            var buffer = Stores.SFXStorage.Get(Get<PlayOnce>(entity).AudioID);
            voice.Reset();
            voice.SetVolume(0.5f);
            voice.Submit(buffer);
            voice.Play();
            if (Get<PlayOnce>(entity).RandomizePitch)
            {
                voice.SetPitch(Rando.Range(-0.1f, 0.1f));
            }
            Playing.Enqueue(voice);


            Destroy(entity);
        }
    }
}