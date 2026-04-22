using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Globalization;
using SFB; 
using Whisper;
using Novacode; 
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
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
    public TMP_Text statusText; 
    public TMP_Text pathText;
    
    public GameObject buttonTemplate;
    public Transform recentFileList;
    public int number;

    string currentTranscription;

    private string selectedPath;
    
    public static FileTranscriptionHandler instance {get; private set;}

    void Awake() {
        instance = this;
        whisperManager.OnProgress += OnProgressHandler;
    }
    
    private void Start() {
        selectFileButton.onClick.AddListener(OpenFileBrowser);
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
    
    
    private IEnumerator LoadAndTranscribe(string path) {

        //Make a copy of the button
        GameObject inst = Instantiate(buttonTemplate,  recentFileList);
        //Getting the ButtonLogic.cs for created Instantiate
        ButtonLogic buttonLogic =  inst.GetComponent<ButtonLogic>();
        statusText = buttonLogic.progress;
        
        inst.transform.SetSiblingIndex(1);
        
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
            buttonLogic.fileNameText.text = nameFile;
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
            
            buttonLogic.button.interactable = false;
            
            //Setting date for display to text
            buttonLogic.duration.text = convertedDuration;
            
            inst.SetActive(true);
            
            //statusText.text = "Transcribing...";
            RunWhisper(clip, buttonLogic);
        }
    }

    private async void RunWhisper(AudioClip clip, ButtonLogic buttonLogic) {
        var res = await whisperManager.GetTextAsync(clip);
        
        if (res != null) {
            currentTranscription = res.Result;
            if (statusText != null) statusText.text = "Done";
            if (saveFileButton != null) saveFileButton.interactable = true;
            buttonLogic.button.interactable = true;
            
            //gives for ButtonLogic result of current transcription
            buttonLogic.transcribe = currentTranscription;
            
        }
    }
    
    private void OnProgressHandler(int progress) {
        if (statusText != null) {
            statusText.text = $"{progress}%";
        }
    }

    public void SaveTranscription(string transcription) {
        if (string.IsNullOrEmpty(currentTranscription)) return;

        var extensions = new[] {
            new ExtensionFilter("Text File", "txt"),
            new ExtensionFilter("Word Document", "docx"),
            new ExtensionFilter("Portable Document Format", "pdf"),
            
        };
        
        var path = StandaloneFileBrowser.SaveFilePanel("Save File", "", "Transcription", extensions);
        
        if (string.IsNullOrEmpty(path)) return;
        
        string extension = Path.GetExtension(path).ToLower();

        try {
            if (extension == ".docx") {
                SaveAsDocx(path, transcription);
            }
            else if (extension == ".pdf") {
                SaveAsPdf(path, transcription);
            }
            else {
                File.WriteAllText(path, transcription);
            }
            Debug.Log("File Saved Successfully!");
        }
        catch (Exception e) {
            Debug.LogError($"Save Failed: {e.Message}");
        }
    }
    
    // Logic for Native Word Files
    private void SaveAsDocx(string path, string text) {
        // Use DocX specifically to avoid confusion
        using (DocX document = DocX.Create(path)) {
            document.InsertParagraph("Transcription Report").Bold().FontSize(18).Alignment = Alignment.center;
            document.InsertParagraph(System.DateTime.Now.ToString("f")).Italic().Alignment = Alignment.center;
            document.InsertParagraph("\n" + text);
            document.Save();
        }
    }

    // Logic for PDF Files
    private void SaveAsPdf(string path, string text) {
        try {
            //Create the document
            PdfDocument document = new PdfDocument();
            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);

            //Define Fonts (1.50 uses XFontStyle, not XFontStyleEx)
            XFont titleFont = new XFont("Arial", 18, XFontStyle.Bold);
            XFont bodyFont = new XFont("Arial", 11, XFontStyle.Regular);

            //Draw Header
            gfx.DrawString("Transcription Report", titleFont, XBrushes.Black, 
                new XRect(0, 40, page.Width, 40), XStringFormats.Center);

            //Content
            XTextFormatter tf = new XTextFormatter(gfx);
            XRect rect = new XRect(40, 100, page.Width - 80, page.Height - 140);
            tf.DrawString(text, bodyFont, XBrushes.Black, rect);

            //Save
            document.Save(path);
            Debug.Log("PDF Saved successfully with PDFSharp 1.50!");
        }
        catch (Exception e) {
            Debug.LogError("PDF Error: " + e.Message);
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