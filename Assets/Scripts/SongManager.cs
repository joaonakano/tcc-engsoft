using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class SongManager : MonoBehaviour
{
    public static SongManager Instance;

    public AudioSource audioSource;

    public Animator wipeAnimator;

    public Lane[] lanes;

    public float songDelayInSeconds;
    public double marginOfError;

    public int inputDelayInMilliseconds;

    public string fileName;

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
    }

    void Start()
    {
        StartCoroutine(LoadMidiFile(fileName));
    }

    IEnumerator LoadMidiFile(string fileName)
    {
        byte[] fileContent = null;
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);

        // Se Android ou Web
        if (filePath.StartsWith("jar") || filePath.StartsWith("http"))
        {
            UnityWebRequest request = UnityWebRequest.Get(filePath);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                fileContent = request.downloadHandler.data;
            }
            else
            {
                Debug.LogError("Erro ao carregar o arquivo MIDI: " + request.error);
            }
        }
        else
        {
            // Maioria das plataformas (Windows, macOS, Linux)
            if (File.Exists(filePath))
            {
                fileContent = File.ReadAllBytes(filePath);
            }
            else
            {
                Debug.LogError("Arquivo MIDI não encontrado em: " + filePath);
            }
        }

        if (fileContent != null)
        {
            using (var stream = new MemoryStream(fileContent))
            {
                midiFile = MidiFile.Read(stream);
                GetDataFromMidi();
            }
        }
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
