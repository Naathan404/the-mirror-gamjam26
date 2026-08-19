using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Minigames.Wires
{
    public class WiresMinigameController : MinigameBaseController
    {
        [Header("Wires MNG Settings")]
        private MinigameType _type = MinigameType.Wires;



        #region Base
        private void Start()
        {

        }
        #endregion


        #region Handle events
        protected override void OnGameStart()
        {
            throw new System.NotImplementedException();
        }

        protected override void OnGameReset()
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }
}
