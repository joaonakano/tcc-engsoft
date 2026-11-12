using UnityEngine;
using UnityEngine.UI;

public class VolumeSelector : MonoBehaviour
{
    public enum VolumeType
    {
        Music,
        SFX
    }


    [Header("Volume Type")]
    [SerializeField] private VolumeType _volumeType;

    [Header("UI Elements")]
    public Toggle[] toggles;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float _minVolume = 0.05f;
    [SerializeField, Range(0f, 1f)] private float _maxVolume = 0.6f;

    private void Start()
    {
        Initialize();
    }

    public void SelectToggle(int selectedIndex)
    {
        SetToggles(selectedIndex);

        float volume = GetVolumeFromIndex(selectedIndex);

        SetAudioVolume(volume);
    }

    private void Initialize()
    {
        float currentVolume = GetAudioVolume();

        int selectedIndex = GetIndexFromVolume(currentVolume);

        SetToggles(selectedIndex);
    }

    private void SetToggles(int selectedIndex)
    {
        for (int i = 0; i < toggles.Length; i++)
            toggles[i].SetIsOnWithoutNotify(i <= selectedIndex);
    }

    private float GetVolumeFromIndex(int index)
    {
        return Mathf.Lerp(
            _minVolume,
            _maxVolume,
            (float)index / (toggles.Length - 1)
        );
    }

    private int GetIndexFromVolume(float volume)
    {
        int closestIndex = 0;
        float closestDifference = Mathf.Abs(volume - GetVolumeFromIndex(0));

        for (int i = 1; i < toggles.Length; i++)
        {
            float difference = Mathf.Abs(volume - GetVolumeFromIndex(i));

            if (difference < closestDifference)
            {
                closestDifference = difference;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private float GetAudioVolume()
    {
        return _volumeType switch
        {
            VolumeType.Music => Settings.MusicVolume,
            VolumeType.SFX => Settings.SfxVolume,
            _ => 1f
        };
    }

    private void SetAudioVolume(float volume)
    {
        switch (_volumeType)
        {
            case VolumeType.Music:
                Settings.MusicVolume = volume;
                break;

            case VolumeType.SFX:
                Settings.SfxVolume = volume;
                break;
        }
    }
}
