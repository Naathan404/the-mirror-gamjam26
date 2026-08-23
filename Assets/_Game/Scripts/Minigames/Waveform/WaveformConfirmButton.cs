using UnityEngine;

namespace Game.Minigames.Waveform
{
    public class ConfirmButton : MonoBehaviour
    {
        [SerializeField] private WaveformMinigameController _controller;

        private void OnMouseDown()
        {
            _controller.OnConfirmPressed();
        }
    }
}