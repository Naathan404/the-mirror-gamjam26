using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using Game.Core;    
using Game.Cameras; 

public enum TutorialState
{
    Phase1_NeedToFlash,
    Phase2_NeedToCharge,
    Phase3_NeedToSolve,
    Completed
}

[System.Serializable]
public class PhaseConfig
{
    [Range(0f, 100f)]
    public float minigameHintChance;
}

[System.Serializable]
public class HintContentConfig
{
    [Tooltip("Danh sách các câu thoại sẽ xuất hiện tuần tự")]
    public LocalizedString[] texts;

    [Tooltip("Tick vào đây nếu muốn nhóm chữ này hiển thị MÀU ĐỎ (Heavy), bỏ tick sẽ ra MÀU TRẮNG (Normal)")]
    public bool isHeavy;

    [HideInInspector]
    public int currentIndex = 0;
}

public class TutorialHintManager : MonoBehaviour
{
    private struct HintData
    {
        public string Text;
        public Color TextColor;

        public HintData(string text, Color color)
        {
            Text = text;
            TextColor = color;
        }
    }

    [Header("Cài đặt Màu sắc")]
    public Color normalColor = Color.white;
    public Color heavyColor = Color.red;

    [Header("Kho nội dung Hint (Localization)")]
    public HintContentConfig flashlightHints;
    public HintContentConfig chargerHints;
    public HintContentConfig minigameHints;

    [Header("Cấu hình Tỉ lệ Minigame")]
    public PhaseConfig phase1Config = new PhaseConfig { minigameHintChance = 20f };
    public PhaseConfig phase2Config = new PhaseConfig { minigameHintChance = 40f };
    public PhaseConfig phase3Config = new PhaseConfig { minigameHintChance = 70f };

    [Header("Vị trí hiển thị (Slots)")]
    public List<TextMeshPro> deskTextSlots;

    public TutorialState currentState = TutorialState.Phase1_NeedToFlash;

    #region Unity Lifecycle & Events
    private void Awake()
    {
       ClearAllHints();
    }

    private void Start()
    {
        GameEvents.OnViewChangeFinished += HandleViewChange;
        GameEvents.OnLightFlashed += HandleLightFlashed;
        GameEvents.OnBatteryChargeCompleted += HandleBatteryChargeCompleted;
        GenerateHintsOnDesk();
    }

    private void OnDestroy()
    {
        GameEvents.OnViewChangeFinished -= HandleViewChange;
        GameEvents.OnLightFlashed -= HandleLightFlashed;
        GameEvents.OnBatteryChargeCompleted -= HandleBatteryChargeCompleted;
    }

    private void HandleViewChange(View targetView)
    {
        if (targetView == View.Mirror)
        {
            GenerateHintsOnDesk();
        }
    }
    #endregion

    #region Main Logic
    public void GenerateHintsOnDesk()
    {
        ClearAllHints();

        bool isTutorialEnabled = PlayerPrefs.GetInt("TutorialEnabled", 1) == 1;

        List<HintData> hintsToDisplay = new List<HintData>();

        if (currentState == TutorialState.Phase1_NeedToFlash)
        {
            AddHintToList(flashlightHints, hintsToDisplay);
        }
        else if (currentState == TutorialState.Phase2_NeedToCharge)
        {
            AddHintToList(chargerHints, hintsToDisplay);
        }

        float chance = GetMinigameChance(currentState);
        if (Random.Range(0f, 100f) < chance)
        {
            AddHintToList(minigameHints, hintsToDisplay);
        }

        if (hintsToDisplay.Count == 0) return;

        List<TextMeshPro> availableSlots = new List<TextMeshPro>(deskTextSlots);
        ShuffleList(availableSlots);

        for (int i = 0; i < hintsToDisplay.Count; i++)
        {
            if (i < availableSlots.Count && availableSlots[i] != null)
            {
                availableSlots[i].text = hintsToDisplay[i].Text;
                availableSlots[i].color = hintsToDisplay[i].TextColor;
            }
        }
    }

    public void FinishTutorial()
    {
        currentState = TutorialState.Completed;

        PlayerPrefs.SetInt("TutorialEnabled", 0);
        PlayerPrefs.Save();

        ClearAllHints();
    }
    #endregion

    #region State Transition (Thăng cấp Phase)
    private void HandleLightFlashed()
    {
        if (currentState == TutorialState.Phase1_NeedToFlash)
        {
            currentState = TutorialState.Phase2_NeedToCharge;
            Debug.Log("[Tutorial] Đã qua Phase 1, chuyển sang Phase 2 (Nhắc Sạc Pin)");
        }
    }

    private void HandleBatteryChargeCompleted()
    {
        if (currentState == TutorialState.Phase2_NeedToCharge)
        {
            currentState = TutorialState.Phase3_NeedToSolve;
            Debug.Log("[Tutorial] Đã qua Phase 2, chuyển sang Phase 3 (Chỉ nhắc Minigame)");
        }
    }
    #endregion

    #region Helpers
    private void ClearAllHints()
    {
        foreach (var slot in deskTextSlots)
        {
            if (slot != null) slot.text = "";
        }
    }

    private void AddHintToList(HintContentConfig config, List<HintData> list)
    {
        string text = GetSequentialLocalizedText(config);
        if (!string.IsNullOrEmpty(text))
        {
            Color c = config.isHeavy ? heavyColor : normalColor;
            list.Add(new HintData(text, c));
        }
    }

    private string GetSequentialLocalizedText(HintContentConfig config)
    {
        if (config.texts == null || config.texts.Length == 0) return "";

        string text = config.texts[config.currentIndex].GetLocalizedString();

        config.currentIndex = (config.currentIndex + 1) % config.texts.Length;

        return text;
    }

    private float GetMinigameChance(TutorialState state)
    {
        if (state == TutorialState.Phase1_NeedToFlash) return phase1Config.minigameHintChance;
        if (state == TutorialState.Phase2_NeedToCharge) return phase2Config.minigameHintChance;
        if (state == TutorialState.Phase3_NeedToSolve) return phase3Config.minigameHintChance;
        return 0f;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    #endregion
}