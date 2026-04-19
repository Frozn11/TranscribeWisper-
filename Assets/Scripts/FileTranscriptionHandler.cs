using UnityEngine;
using UnityEngine.UI;
using LightSide;
using System.Collections;
using SFB; 
using Whisper;
using Whisper.Utils; 
using UnityEngine.Networking;
using System.IO;

public class FileTranscriptionHandler : MonoBehaviour
{
    [Header("Whisper Reference")]
    public WhisperManager whisperManager;
    
    [Header("UI Controls")]
    public Button selectFileButton;
    public Button saveFileButton;
    public UniText outputText;
    public UniText statusText; 
    public ScrollRect scroll;

    string currentTranscription;
    private void Start()
    {
        selectFileButton.onClick.AddListener(OpenFileBrowser);
        saveFileButton.onClick.AddListener(SaveTranscription);
        if (saveFileButton != null) saveFileButton.interactable = false;
    }

    public void OpenFileBrowser()
    {
        var extensions = new[] { new ExtensionFilter("Audio Files", "mp3", "wav", "ogg", "m4a") };
        var paths = StandaloneFileBrowser.OpenFilePanel("Select File", "", extensions, false);
        
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            StartCoroutine(LoadAndTranscribe(paths[0]));
        }
    }

    private IEnumerator LoadAndTranscribe(string path)
    {
        statusText.Text = "Loading file...";

        // Use AudioType.UNKNOWN to let Unity try to guess the format from the extension
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.UNKNOWN))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                statusText.Text = "Error: " + uwr.error;
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
            
            statusText.Text = "Transcribing...";
            RunWhisper(clip);
        }
    }

    private async void RunWhisper(AudioClip clip)
    {
        var res = await whisperManager.GetTextAsync(clip);
        
        if (res != null) {
            currentTranscription = res.Result;
            if (outputText != null) outputText.Text = currentTranscription;
            if (statusText != null) statusText.Text = "Transcription Complete";
            if (saveFileButton != null) saveFileButton.interactable = true;
            
            // Automatically scroll to the bottom if a ScrollRect is assigned
            if (scroll != null)
                UiUtils.ScrollDown(scroll);
        }
    }

    public void SaveTranscription() {
        if (string.IsNullOrEmpty(currentTranscription)) return;

        var extensions = new[] {
            new ExtensionFilter("Text File", "txt"),
            new ExtensionFilter("Word Document", "docx")
        };
        
        var path = StandaloneFileBrowser.SaveFilePanel("Save File", "", "Transcription", extensions);

        if (!string.IsNullOrEmpty(path)) {
            try {
                File.WriteAllText(path, currentTranscription);
                statusText.Text = "Transcription Saved";
            }
            catch (System.Exception e) {
                statusText.Text = $"Failed {e.Message}";
            }
        }
    }
}