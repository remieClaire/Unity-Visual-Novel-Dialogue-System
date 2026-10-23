using Ink.Runtime;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    // class instance variables
    public static DialogueManager instance { get; private set; }
    private DialogueSystem dialogueSystem => DialogueSystem.instance;

    // holds dialogue for the current scene
    private Story currentStory = null;

    // ---state-tracking boolean vars---
    // lets other classes know the status of the conversation
    public bool isPlayingStory = false;
    // notifies system to progress 
    public bool advanceDialogue = false;
    // pauses execution while user is making a choice
    public bool makeChoice = false;

    // ---auto-reading vars---
    public bool auto { get; set; } = false;
    public float speed { get; set; } = 1f;

    // setup class instance
    void Awake()
    {
        if (instance != null && instance != this)
        {
            DestroyImmediate(gameObject);
        }

        instance = this;
    }

    void Start()
    {
        dialogueSystem.onUserPromptNext += OnUserPromptNext;
    }

    void Update()
    { 

        if (advanceDialogue && dialogueSystem.showNextLine && !makeChoice)
            // start the dialogue
            RunStory();
        
    }

    private void OnUserPromptNext()
    {
        advanceDialogue = true;
    }

    public void StartStory(TextAsset inkJSON)
    {
        // prepare UI panel & load current ink story
        currentStory = dialogueSystem.EnterDialogueMode(inkJSON);    
    }

    private void RunStory()
    {
        if (currentStory.canContinue)
        {
            // set text for current dialogue line
            dialogueSystem.DisplayDialogue(currentStory.Continue());
            isPlayingStory = true;

            // display choices, if any, for this dialogue line
            if (currentStory.currentChoices.Count > 0)
            {
                makeChoice = true;
                dialogueSystem.DisplayChoices();
            }
            
            // handle ink tags
            if (currentStory.currentTags.Count > 0)
                dialogueSystem.HandleTags(currentStory.currentTags);
            

        }
        else
        {
            StartCoroutine(dialogueSystem.ExitDialogueMode());
        }
       
    }

    public void ContinueStory()
    {
        RunStory();
    }

    public void ToggleAuto()
    {
       if (auto)
        {
            dialogueSystem.EnableAuto();
        }
        else
        {
            if (!dialogueSystem.autoIsRunning)
            {
                dialogueSystem.EnableAuto();
            }
            else
            {
                dialogueSystem.DisableAuto();
            }
        }

        auto = false;
    }
}
