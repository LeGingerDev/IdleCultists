using Audio.Settings.Managers;
using LGD.Core.Application;
using LGD.Core.Events;
using LGD.UIelements.Panels;
using LGD.Utilities.UI.UIComponents;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.UI;

public class SettingsMenu : SlidePanel
{
    [FoldoutGroup("Settings Elements")]
    public Slider masterVolumeSlider;
    [FoldoutGroup("Settings Elements")]
    public Slider musicVolumeSlider;
    [FoldoutGroup("Settings Elements")]
    public Slider sfxVolumeSlider;
    [FoldoutGroup("Settings Elements")]
    public TMP_Dropdown resolutionDropdown;
    [FoldoutGroup("Settings Elements")]
    public CustomToggle fullscreenToggle;

    protected override void Start()
    {
        base.Start();
        InitialiseValues();
    }

    public void InitialiseValues()
    {
        //TODO: Fix error. Cbb at the moment

        fullscreenToggle.Initialise(SettingsManager.Instance.fullscreenToggle,
            () => SettingsManager.Instance.SetIsFullscreen(true),
            () => SettingsManager.Instance.SetIsFullscreen(false),
            false);

        var options = ResolutionUtilities.GetResolutionOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = SettingsManager.Instance.lastResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(index =>
        {
            SettingsManager.Instance.SetResolution(index);
        });


    }

    public void InitialiseOnOpen()
    {
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);

        musicVolumeSlider.value = AudioSettingsManager.Instance.BgmVolume;
        sfxVolumeSlider.value = AudioSettingsManager.Instance.SfxVolume;
        masterVolumeSlider.value = AudioSettingsManager.Instance.MasterVolume;
    }

    [Topic(ApplicationEventIds.ON_SETTINGS_SELECTED)]
    public void Open(object sender, bool isOpenedByKey)
    {
        ShowPanel();
        InitialiseOnOpen();
    }

    public void DeinitialiseOnClose()
    {
        musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        sfxVolumeSlider.onValueChanged.RemoveListener(SetSFXVolume);
        masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);

        AudioSettingsManager.Instance.SaveSettings();
    }

    public void SetMusicVolume(float volume) => AudioSettingsManager.Instance.BgmVolume = volume;
    public void SetSFXVolume(float volume) => AudioSettingsManager.Instance.SfxVolume = volume;
    public void SetMasterVolume(float volume) => AudioSettingsManager.Instance.MasterVolume = volume;

    protected override void OnOpen()
    {
        InitialiseOnOpen();
    }

    protected override void OnClose()
    {
        DeinitialiseOnClose();
    }
}
