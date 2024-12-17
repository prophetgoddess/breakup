using System.Collections;
using Ball;
using MoonTools.ECS;
using MoonWorks.Audio;

public class Audio : MoonTools.ECS.System
{
    Filter SFXFilter;
    AudioDevice AudioDevice;

    StreamingVoice MusicVoice;
    AudioDataOgg MusicData;


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
        System.Console.WriteLine(Content.Music.music);
        MusicData = AudioDataOgg.Create(audioDevice);
        MusicData.Open(File.ReadAllBytes(Content.Music.music));

        MusicVoice = new StreamingVoice(audioDevice, MusicData.Format);
        MusicVoice.Loop = true;
        MusicVoice.SetVolume(0.0f);
        MusicVoice.Load(MusicData);
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
            voice.SetVolume(0.66f);
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