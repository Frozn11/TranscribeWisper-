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
    [Space()]
    
    [Header("Button Data")]
    public string transcribe;

    public void Open() {
        transcription.text = $"{fileNameText.text}\n<size=17.46><color=#050505>{date.text}</color></size>\n\n<size=23.1>{transcribe}</size>";
        FileTranscriptionHandler.instance.saveFileButton.onClick.RemoveAllListeners();
        FileTranscriptionHandler.instance.saveFileButton.onClick.AddListener(() => {
            FileTranscriptionHandler.instance.SaveTranscription(transcribe);
        });
        
    }

}
