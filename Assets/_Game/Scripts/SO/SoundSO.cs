using System.Collections.Generic;
using UnityEngine;

public enum SoundName
{
    ButtonClick,
    LightFlash,
    Charge,
    ChargeCompleted,
    Wire_Success,
    Wire_Fail,
    Card_Flip_Up,
    Card_Flip_Down,
    Card_Matched,
    Maze_Moving,
    Maze_Success,
    Maze_Fail,
    Word_Success,
    Word_Fail,
    Pick_Clue,
    Button3DClick,
    Entity_ChangeState,

    Ending_LockUnlock,
    Ending_Walking,
    Ending_Background,
    Menu_Whisper,
    Menu_Glitch,

    Entity_Jumpscare,

    Lock_Combination,

    Minigame_Fail,
    Minigame_Open,
    Minigame_Win,

    View_Change,

    Lazors_Rotate,
    Lazors_Gun,
    Laser_Light,
    Laser_Block,


    BGM_Gameplay_1,
    BGM_Gameplay_2,
    BGM_Gameplay_3,

    Key_Collect,
    Key_Unlock,

    Waveform_Fail,
    Waveform_Rotate,

    None
}

[CreateAssetMenu(fileName = "NewSoundData", menuName = "Audio/Sound Data")]
public class SoundSO : ScriptableObject
{
    public SoundName soundName;
    public List<AudioClip> audioClips;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float minPitch = 0.95f;
    [Range(0.1f, 3f)] public float maxPitch = 1.05f;

    private void OnValidate()
    {
        if (minPitch > maxPitch)
        {
            maxPitch = minPitch;
        }
    }
}


