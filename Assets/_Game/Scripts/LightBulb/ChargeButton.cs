using DG.Tweening;
using Game.Core;
using UnityEngine;

public class ChargeButton : MonoBehaviour
{
    private void OnMouseDown()
    {
        GameEvents.RaiseBatteryChargeStarted();

        transform.DOPunchPosition(new Vector3(0f, 0f, 0.3f), 0.4f);
    }
}
