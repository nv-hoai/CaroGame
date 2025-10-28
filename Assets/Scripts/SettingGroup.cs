using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingGroup : MonoBehaviour
{
    public TMP_Dropdown graphicQualityDropdown;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    void Start()
    {
        GameManager.Instance.SetTMPDropdown(graphicQualityDropdown);
        AudioManager.Instance.SetMasterSlider(masterSlider);
        AudioManager.Instance.SetMusicSlider(musicSlider);
        AudioManager.Instance.SetSFXSlider(sfxSlider);
    }
}
