using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds named groups of <see cref="AudioClip"/>s and returns a random clip
/// from a group by name, used by <see cref="SoundEffectManager"/>.
/// </summary>
public class SoundEffectLibrary : MonoBehaviour
{
    [SerializeField] private SoundEffectGroup[] soundGroups;
    private Dictionary<string, List<AudioClip>> soundDictionary;

    private void Awake()
    {
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        soundDictionary = new Dictionary<string, List<AudioClip>>();
        foreach (SoundEffectGroup group in soundGroups)
        {
            soundDictionary[group.name] = group.audioClips;
        }
    }

    public AudioClip GetRandomClip(string name)
    {
        if(soundDictionary.ContainsKey(name))
        {
            List<AudioClip> audioClips = soundDictionary[name];
            if(audioClips.Count > 0)
            {
                int index = Random.Range(0, audioClips.Count);
                return audioClips[index];
            }
        }
        return null;
    }
}

[System.Serializable]
public struct SoundEffectGroup
{
    public string name;
    public List<AudioClip> audioClips;
}