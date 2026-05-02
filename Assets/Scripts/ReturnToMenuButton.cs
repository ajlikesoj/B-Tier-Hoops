using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Attach to a UI Button to make it load the MainMenu scene when clicked.
/// We can't add the click handler from the editor-time scene builder because lambda listeners
/// added via `Button.onClick.AddListener` are non-persistent and aren't serialized into the
/// scene file — they're gone by the time the scene is loaded at runtime. This component wires
/// the listener in Start so it actually exists when the user clicks.
/// </summary>
[RequireComponent(typeof(Button))]
public class ReturnToMenuButton : MonoBehaviour
{
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        Debug.Log($"[BTierHoops] Return-to-Menu clicked → loading '{MatchSettings.MainMenuSceneName}'");
        try
        {
            SceneManager.LoadScene(MatchSettings.MainMenuSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BTierHoops] Couldn't load '{MatchSettings.MainMenuSceneName}': {e.Message}. " +
                           $"Make sure Assets/Scenes/{MatchSettings.MainMenuSceneName}.unity exists and is in File > Build Settings (run 'BTierHoops > Setup Build Settings').");
        }
    }
}
