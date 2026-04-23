using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonLogic : MonoBehaviour {
    [Header("Button stuff")] 
    public Button button;
    public TMP_Text fileNameText;
    public TMP_Text date;
    public TMP_Text duration;
    public TMP_Text progress;
    public TMP_Text transcription;
    public GameObject panel;
    
    [Header("Three dots button references")]
    public Button openThreeDotButton;
    public Button exportThreeDotButton;
    public Button renameThreeDotButton;
    public Button deleteThreeDotButton;
    
    bool isOpenPanel = false;
    [Space()]
    
    [Header("Button Data")]
    public string transcribe;

    void Start() {
        panel.SetActive(false);
        openThreeDotButton.interactable = false;
        exportThreeDotButton.interactable = false;
        renameThreeDotButton.interactable = false;
        
        openThreeDotButton.onClick.AddListener(() => {
            OpenTranscribe();
        });
        exportThreeDotButton.onClick.AddListener(() => {
            FileTranscriptionHandler.instance.SaveTranscription(transcribe);
        });
        renameThreeDotButton.onClick.AddListener(() => {
            Debug.Log("ahhh yes rename the file");
        });
        deleteThreeDotButton.onClick.AddListener(() => {
            Debug.Log("yeah yeah yeah delete the file");
        });
    }

    public void OpenTranscribe() {
        transcription.text = $"{fileNameText.text}\n<size=17.46><color=#050505>{date.text}</color></size>\n\n<size=23.1>{transcribe}</size>";
        FileTranscriptionHandler.instance.saveFileButton.onClick.RemoveAllListeners();
        FileTranscriptionHandler.instance.saveFileButton.onClick.AddListener(() => {
            FileTranscriptionHandler.instance.SaveTranscription(transcribe);
        });
    }

    public void OpenPanel() {
        isOpenPanel = !isOpenPanel;
        panel.SetActive(isOpenPanel);

    }

}
