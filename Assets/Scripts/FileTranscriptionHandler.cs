using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Globalization;
using SFB; 
using Whisper;
using Whisper.Utils; 
using UnityEngine.Networking;
using System.IO;
using TMPro;

public class FileTranscriptionHandler : MonoBehaviour
{
    [Header("Whisper Reference")]
    public WhisperManager whisperManager;
    
    [Header("UI Controls")]
    public Button selectFileButton;
    public Button saveFileButton;
    public Button TranscribeButton;
    public TMP_Text outputText;
    public TMP_Text statusText; 
    public TMP_Text pathText;
    public ScrollRect scroll;
    
    public GameObject buttonTemplate;
    public Transform recentFileList;
    public int Number;

    string currentTranscription;

    private string selectedPath;

    void Awake() {
        whisperManager.OnProgress += OnProgressHandler;
    }
    
    private void Start() {
        selectFileButton.onClick.AddListener(OpenFileBrowser);
        saveFileButton.onClick.AddListener(SaveTranscription);
        if (saveFileButton != null) saveFileButton.interactable = false;
        if (TranscribeButton != null) TranscribeButton.interactable = false;
    }

    public void OpenFileBrowser() {
        var extensions = new[] { new ExtensionFilter("Audio Files", "mp3", "wav", "ogg", "m4a") };
        var paths = StandaloneFileBrowser.OpenFilePanel("Select File", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            //StartCoroutine(LoadAndTranscribe(paths[0]));
            selectedPath = paths[0];
            pathText.text = selectedPath;
            TranscribeButton.interactable = true;
        }
    }

    public void StartTranscribe() {

        StartCoroutine(LoadAndTranscribe(selectedPath));
    }

    public void UpdatedResentFiles() {
        if (recentFileList.childCount > 1) {
            for (int i = recentFileList.childCount - 1; i >= 0; i--) {
                Destroy(recentFileList.GetChild(i).gameObject);
            }
        }

        for (int i = 0; i < Number; i++) {
            int index = i;
            GameObject inst = Instantiate(buttonTemplate, recentFileList);
            AddTextAndListener(inst, index);
            
            inst.SetActive(true);
        }
    }

    void AddTextAndListener(GameObject inst, int index) {
        
    }
    
    private IEnumerator LoadAndTranscribe(string path) {

        //Make a copy of the button
        GameObject inst = Instantiate(buttonTemplate,  recentFileList);
        //Getting the ButtonLogic.cs for created Instantiate
        ButtonLogic buttonLogic =  inst.GetComponent<ButtonLogic>();
        statusText = buttonLogic.progress;
        
        statusText.text = "Loading file...";

        // Use AudioType.UNKNOWN to let Unity try to guess the format from the extension
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.UNKNOWN)) {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                statusText.text = "Error: " + uwr.error;
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
            

            
            //Getting the name of the file
            string nameFile = Path.GetFileNameWithoutExtension(selectedPath);
            
            //Setting the name of the button display to text
            buttonLogic.name.text = nameFile;
            //Getting the time right now and setting it to display to text
            buttonLogic.date.text = DateTime.Now.ToString("MMM dd, yyyy h:mm:ss tt", CultureInfo.GetCultureInfo("en-US"));
            
            float durationSeconds = clip.length;
            //Using TimeSpan for easy formating
            System.TimeSpan time = System.TimeSpan.FromSeconds(durationSeconds);
            string convertedDuration = "";

            if (durationSeconds < 60) {
                convertedDuration = string.Format("{0:D2}s", time.Seconds);
            }
            else if (time.TotalMinutes < 60) {
                convertedDuration = string.Format("{0:D2}m {1:D2}s", time.Minutes, time.Seconds);
            }
            else {
                convertedDuration = string.Format("{0:D2}h {1:D2}m", (int)time.TotalHours, time.Minutes);
            }
            
            //Setting date for display to text
            buttonLogic.duration.text = convertedDuration;
        
            inst.SetActive(true);
            
            //statusText.text = "Transcribing...";
            RunWhisper(clip);
        }
    }

    private async void RunWhisper(AudioClip clip) {
        var res = await whisperManager.GetTextAsync(clip);
        
        if (res != null) {
            currentTranscription = res.Result;
            if (outputText != null) outputText.text = currentTranscription;
            if (statusText != null) statusText.text = "ye";
            if (saveFileButton != null) saveFileButton.interactable = true;
            
            // Automatically scroll to the bottom if a ScrollRect is assigned
            if (scroll != null)
                UiUtils.ScrollDown(scroll);
        }
    }
    
    private void OnProgressHandler(int progress) {
        if (statusText != null) {
            statusText.text = $"{progress}%";
        }
    }

    public void SaveTranscription() {
        if (string.IsNullOrEmpty(currentTranscription)) return;

        var extensions = new[] {
            new ExtensionFilter("Text File", "txt"),
            new ExtensionFilter("Word Document", "docx"),
            new ExtensionFilter("Portable Document Format", "pdf"),
            
        };
        
        var path = StandaloneFileBrowser.SaveFilePanel("Save File", "", "Transcription", extensions);

        if (!string.IsNullOrEmpty(path)) {
            try {
                File.WriteAllText(path, currentTranscription);
                //statusText.text = "Transcription Saved";
            }
            catch (System.Exception e) {
                statusText.text = $"Failed {e.Message}";
            }
        }
    }
    
    private void OnDestroy() {
        // Clean up event subscription when object is destroyed
        if (whisperManager != null) {
            whisperManager.OnProgress -= OnProgressHandler;
        }
    }

    public void TextDebug(string T) {
        Debug.Log(T);
    }
}