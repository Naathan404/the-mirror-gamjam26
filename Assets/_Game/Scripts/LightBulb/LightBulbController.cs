using System;
using System.Collections;
using UnityEngine;
using Game.Core;
using Game.Managers;

public class LightBulbController : MonoBehaviour
{
    [Header("Light Bulb Settings")]
    [SerializeField] private int _maxBatteryCount = 1;
    [SerializeField] private int _batteryCount = 0;
    [Tooltip("Thời gian pin cần sạc để có thể sử dụng đèn")]
    [SerializeField] private float _batteryLife = 20f;

    [Header("Effects")]
    [SerializeField] private LightFlashEffect _lightFlashEffect;


    [SerializeField] private float _batteryChargingProcess;
    private bool _isCharging = false;
    private Coroutine _chargingRoutine;

    public event Action<float> OnBatteryProgressChanged;

    private void Start()
    {
        _batteryCount = 0;
        _batteryChargingProcess = 0f;
        OnBatteryProgressChanged?.Invoke(0f);

        GameEvents.OnLightFlashed += TurnOnLight;
        GameEvents.OnBatteryChargeStarted += StartChargeBattery;
    }

    private void OnDestroy()
    {
        GameEvents.OnLightFlashed -= TurnOnLight;
        GameEvents.OnBatteryChargeStarted -= StartChargeBattery;
    }

    public void TurnOnLight()
    {
        if (_batteryCount <= 0)
            return;

        _batteryCount--;

        if (_lightFlashEffect != null)
        {
            _lightFlashEffect.PlayLightFlash();
        }
    }

    public void StartChargeBattery()
    {
        if (_batteryCount >= _maxBatteryCount || _isCharging)
            return;

        if (_chargingRoutine != null)
        {
            StopCoroutine(_chargingRoutine);
        }

        _chargingRoutine = StartCoroutine(ChargingRoutine());
    }

    private IEnumerator ChargingRoutine()
    {
        _isCharging = true;

        OnBatteryProgressChanged?.Invoke(0f);

        while (_batteryChargingProcess < _batteryLife)
        {
            _batteryChargingProcess += Time.deltaTime;

            float progress = Mathf.Clamp01(_batteryChargingProcess / _batteryLife);

            OnBatteryProgressChanged?.Invoke(progress);
            yield return null;
        }

        OnBatteryProgressChanged?.Invoke(1f);

        ChargeCompleted();
    }

    private void ChargeCompleted()
    {
        _isCharging = false;
        _batteryCount++;
        _batteryChargingProcess = 0f;

        GameEvents.RaiseBatteryChargeCompleted();

        _chargingRoutine = null;
    }
}