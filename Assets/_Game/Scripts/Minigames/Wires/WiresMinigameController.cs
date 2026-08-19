using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Minigames.Wires
{
    public class WiresMinigameController : MonoBehaviour
    {
        [Header("Wires MNG Settings")]
        private MinigameType _type = MinigameType.Wires;


        private int _passcodeNumber;

        #region Base
        private void Start()
        {
            GameEvents.OnPasscodeGenerated += HandlePasscodeGenerated;
        }
        #endregion


        #region Handle events
        private void HandlePasscodeGenerated(Dictionary<MinigameType, int> dic)
        {
            dic.TryGetValue(_type, out _passcodeNumber);
            Debug.Log("");
        }
        #endregion
    }
}
