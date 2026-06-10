using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that holds the full ordered list of <see cref="StoryBeat"/> assets
/// that make up the game's narrative sequence.
/// </summary>
[CreateAssetMenu(fileName = "StoryScript", menuName = "Story/StoryScript")]
public class StoryScript : ScriptableObject
{
    public List<StoryBeat> beats;
}
