using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Linq;
using UnityEngine;

public class SongManager : MonoBehaviour
{
    public static SongManager Instance;

    public AudioSource audioSource;

    public Animator wipeAnimator;

    public Lane[] lanes;

    public float songDelayInSeconds;
    public double marginOfError;

    public int inputDelayInMilliseconds;

    public string filePath;
    public float noteTime;
    public float noteSpawnX;
    public float noteTapX;
    public float noteDespawnX
    {
        get
        {
            return noteTapX - (noteSpawnX - noteTapX);
        }
    }

    public static MidiFile midiFile;

    void Awake()
    {
        Instance = this;
        ReadFromFile();
    }

    private void ReadFromFile()
    {
        midiFile = MidiFile.Read(filePath);
        GetDataFromMidi();
    }

    private void GetDataFromMidi()
    {
        var notes = midiFile.GetNotes();
        var array = new Melanchall.DryWetMidi.Interaction.Note[notes.Count()];
        notes.CopyTo(array, 0);

        foreach (var lane in lanes) lane.SetTimeStamps(array);

        //Invoke(nameof(StartSong), songDelayInSeconds);
    }

    void Update()
    {
        if (SongHasEnded)
        {
            wipeAnimator.SetTrigger("WipeOut");
        }
    }

    public bool SongHasEnded
    {
        get
        {
            return audioSource.timeSamples >= audioSource.clip.samples;
        }
    }

    public void StartSong()
    {
        audioSource.Play();
    }

    public static double GetAudioSourceTime()
    {
        return (double)Instance.audioSource.timeSamples / Instance.audioSource.clip.frequency;
    }
}
