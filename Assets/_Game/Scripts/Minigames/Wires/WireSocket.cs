using Game.Core;
using Game.Minigames.Maze;
using UnityEngine;

namespace Game.Minigames.Wires
{
    [RequireComponent(typeof(Collider))]
    public sealed class WireSocket : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private WiresConfig _config;

        [Header("Socket Settings")]
        [SerializeField] private WireSide _side = WireSide.Left;

        public ColorId ColorId = ColorId.Red;
        public bool IsConnected = false;

        [Header("Visual")]
        [SerializeField] private Renderer _socketRenderer;
        [SerializeField] private Transform _anchor;

        public WireSide Side => _side;
        public Vector3 OriginalScale => _config.Scale;

        public Vector3 AnchorPosition => _anchor != null ? _anchor.position : transform.position;

        public void Initial(WireSide side, ColorId colorId = ColorId.Red)
        {
            _side = side;
            SetColor(colorId);
        }

        public void SetColor(ColorId id)
        {
            ColorId = id;

            var color = _config.GetColorById(id);
            if (_socketRenderer != null)
            {
                var m = new MaterialPropertyBlock();
                _socketRenderer.GetPropertyBlock(m);
                m.SetColor("_BaseColor", color);
                _socketRenderer.SetPropertyBlock(m);
            }
        }

        public void SetHidden()
        {
            var color = Color.black;
            if (_socketRenderer != null)
            {
                var m = new MaterialPropertyBlock();
                _socketRenderer.GetPropertyBlock(m);
                m.SetColor("_BaseColor", color);
                _socketRenderer.SetPropertyBlock(m);
            }
        }

        public void ResetSocket()
        {
            IsConnected = false;
        }
    }

    public enum WireSide
    {
        Left = 0,
        Right = 1
    }
}