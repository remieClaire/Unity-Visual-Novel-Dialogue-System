using UnityEngine;
using UnityEngine.EventSystems;
using Ink.Runtime;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    // singleton of this class 
    public static DialogueSystem instance { get; private set; }
    // reference to DialogueManager
    private DialogueManager dialogueManager;

    // ---dialogue UI---
    [SerializeField] GameObject dialoguePanel;
    [SerializeField] TextMeshProUGUI dialogueText;
    [SerializeField] TextMeshProUGUI nameText;

    // ---ink file content---
    private Story currentStory;

    // ink tags
    const string SPEAKER_TAG = "speaker";
    const string BASE_SPRITE_TAG = "base sprite";
    // const string BODY_SPRITE_TAG = "body sprite";
    // const string EXPRESSION_SPRITE = "expression";

    // ---dialogue control----
    // bools to keep track of state
    public bool dialogueIsPlaying => dialogueManager.isPlayingStory;
    private bool dialogueIsTyping => typingDialogueCoroutine != null;

    // player input tracking
    public delegate void DialogueSystemEvent();
    public event DialogueSystemEvent onUserPromptNext;

    // ---typewriter effect related---
    private Coroutine typingDialogueCoroutine;

    // speed vars
    public float speed { get { return baseSpeed * speedMultiplier; } set { speedMultiplier = value; } }
    private const float baseSpeed = 1;
    private float speedMultiplier = 1;

    // chars per frame vars
    public int charsPerCycle { get { return speed <= 2f ? characterMultiplier : speed <= 2.5f ? characterMultiplier * 2 : characterMultiplier * 3; } }
    private int characterMultiplier = 1;

    public bool showNextLine = true;

    // ---auto reader vars---
    [SerializeField] Toggle autoToggle;

    private bool autoOn => dialogueManager.auto;

    public bool autoIsRunning => autoReadCoroutine != null;
    private Coroutine autoReadCoroutine;

    private const int DEFAULT_CHARS_PER_SECOND = 18;
    private const float READ_TIME_PADDING = 0.5f;
    private const float MAX_READ_TIME = 99f;
    private const float MIN_READ_TIME = 1f;

    // ---choices UI---
    [SerializeField] GameObject[] choiceButtons; // can have any number of choices so long as UI supports it
    private TextMeshProUGUI[] choicesText;

    // ---Unity Functions---
    // setup class instance
    void Awake()
    {
        if (instance != null && instance != this)
        {
            DestroyImmediate(gameObject);
        }

        instance = this;
    }

    void Update()
    {
        // Debug.Log(autoOn);
    }

    // setup variables
    void Start()
    {
        dialogueManager = DialogueManager.instance;

        dialoguePanel.SetActive(false);

        // get all of our possible choices
        choicesText = new TextMeshProUGUI[choiceButtons.Length];
        int index = 0; 
        foreach (GameObject btn in choiceButtons)
        {
            choicesText[index] = btn.GetComponentInChildren<TextMeshProUGUI>();
            index++;
        }
    }

    // ---User Input Functions---
    public void OnUserPromptNext()
    {
        onUserPromptNext?.Invoke();
    }

    // ---Dialogue Start & Stop Functions---
    // load ink story & setup dialogue panel
    public Story EnterDialogueMode(TextAsset inkJSON)
    {
        // set up UI
        dialoguePanel.SetActive(true);
        // convert JSON into ink story
        currentStory = new Story(inkJSON.text);

        // send story back to manager to handle 
        return currentStory;
    }

    // ends dialogue & hides UI
    public IEnumerator ExitDialogueMode()
    {
        yield return new WaitForSeconds(0.2f);

        // update dialogue manager on status
        dialogueManager.isPlayingStory = false;
        
        // handle dialogue system vars
        typingDialogueCoroutine = null; // ensures no memory leak

        dialogueText.text = "";
        dialoguePanel.SetActive(false);
    }

    // ---Dialogue Running Functions---
    // takes dialogue & displays text to UI
    public void DisplayDialogue(string line)
    {
        // prepare text & text UI object
        // reset (ensures first line is shown)
        dialogueText.ForceMeshUpdate();

        // set text to full line, but set visible characters to 0
        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;

        // **hide dialogue choices if there are any once user is done
        HideChoices();
        
        // mark user input as received 
        dialogueManager.advanceDialogue = false;

        // send line to be built   
        typingDialogueCoroutine = StartCoroutine(TypeDialogue(line));
    }

    // handles typing effect
    private IEnumerator TypeDialogue(string line)
    {
        // prevent next line from showing before dialogue has finished typing
        showNextLine = false;
        
        // while the entire text has not been shown
        while (dialogueText.maxVisibleCharacters < line.ToCharArray().Count())
        {
            // add letters incrementally
            dialogueText.maxVisibleCharacters += charsPerCycle;

            // if the user wants to skip dialogue
            if (dialogueManager.advanceDialogue)
            {
                // display entire line
                dialogueText.maxVisibleCharacters = line.Length;
                // mark user input as received
                dialogueManager.advanceDialogue = false;

                break;
            }
            
            yield return new WaitForSeconds(0.02f);
        }

        // next dialogue line can now be shown
        showNextLine = true;

        // prevent memory leak
        typingDialogueCoroutine = null;
    }
    
    // handles dialogue metadata (i.e. name, sprites)
    public void HandleTags(List<string> currentTags)
    {
        // loop through each tag and handle accordingly
        foreach (string tag in currentTags)
        {
            // parse the tag
            string[] splitTag = tag.Split(':');
            if (splitTag.Length != 2)
            {
                Debug.LogError("Tag could not be appropriately parsed: " + tag);
            }
            string tagKey = splitTag[0].Trim();
            string tagValue = splitTag[1].Trim();

            // handle tag depending on type
            switch (tagKey)
            {
                case SPEAKER_TAG:
                    nameText.text = tagValue;
                    break;
                case BASE_SPRITE_TAG:
                    Debug.Log("base sprite=" + tagValue);
                    break;
                default:
                    Debug.LogWarning("Tag came in but was not handled: " + tag);
                    break;
            }
        }
    }

    // ---Dialogue Choice Functions---
    // display choices to UI
    public void DisplayChoices()
    {
        // prep UI & choices
        // get any choices offered in our ink file
        List<Choice> currentChoices = currentStory.currentChoices;
        
        // error check: ensure UI can support number of choices coming in
        if (currentChoices.Count > choiceButtons.Length)
            Debug.LogError("More choices were given than the UI can support. Number of choices given: " + currentChoices.Count);


        // loop through all currently available choices & initialize
        int index = 0; 
        foreach (Choice choice in currentChoices)
        {
            // make button visible
            choiceButtons[index].gameObject.SetActive(true);
            choicesText[index].text = choice.text;
            index++;
        }

        // go through remaining choices & ensure they are hidden
        for (int i = index; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].gameObject.SetActive(false);
        }

        // allows player to also use arrow keys to make choice
        StartCoroutine(InitKeyboard());
    }

    private void HideChoices()
    {
        foreach (GameObject choiceBtn in choiceButtons)
        {
            choiceBtn.SetActive(false);
        }
    }
    
    // record user's choice
    public void MakeChoice(int choiceIndex)
    {
        if (!dialogueIsTyping)
            // confirms choice in Ink
            currentStory.ChooseChoiceIndex(choiceIndex);
        
        // let manager know user is done making a choice
        dialogueManager.makeChoice = false;
        // continue story if there is any more dialogue
        dialogueManager.ContinueStory();
    }
    
    // "unlocks" keyboard so player can use keys
    private IEnumerator InitKeyboard()
    {
        // clear Unity's Event System first
        EventSystem.current.SetSelectedGameObject(null);
        // wait for one frame before setting choice
        yield return new WaitForEndOfFrame();
        // set user choice
        EventSystem.current.SetSelectedGameObject(choiceButtons[0].gameObject); 
    }

    // ---Auto Reader Functions---
    public void EnableAuto()
    {
        if (autoIsRunning)
            return;

        // change color of toggle (on)
        ColorBlock cb = autoToggle.colors;

        cb.normalColor = new Color(1, 0.5f, 0.31f, 1);
        cb.selectedColor = new Color(1, 0.5f, 0.31f, 1);

        autoToggle.colors = cb;

        // start auto reading function & let manager know it is on
        dialogueManager.auto = true;
        autoReadCoroutine = StartCoroutine(AutoRead());
    }

    public void DisableAuto()
    {
        if (!autoIsRunning)
            return;

        // change color of toggle (off)
        ColorBlock cb = autoToggle.colors;

        cb.normalColor = new Color(1, 1, 1, 1);
        cb.selectedColor = new Color(1, 1, 1, 1);
        
        autoToggle.colors = cb;

        // stop auto reading function
        StopCoroutine(autoReadCoroutine);
        autoReadCoroutine = null;

        // let manager know auto reader is off
        dialogueManager.auto = false;
    }

    private IEnumerator AutoRead()
    {
        yield return new WaitForEndOfFrame();

        // do nothing if there is no dialogue running
        if (!dialogueIsPlaying)
        {
            DisableAuto();
            yield break;
        }
        
        if (!dialogueIsTyping && currentStory.currentText != string.Empty)
        {
            OnUserPromptNext();
        }

        while (dialogueIsPlaying)
        {
            // if we're not auto reading, wait for dialogue to finish typing
            if (!autoOn)
            {
                while (!dialogueIsTyping)
                    yield return null;

                float timeStarted = Time.time;

                // wait for dialogue to finish typing out line
                while (dialogueIsTyping)
                    yield return null;

                // calculate remainder of time based on amount of chars in line
                float charsToRead = dialogueText.textInfo.characterCount;
                float timeToRead = Mathf.Clamp((float)charsToRead / DEFAULT_CHARS_PER_SECOND, MIN_READ_TIME, MAX_READ_TIME);

                float currDuration = Time.time - timeStarted;
                timeToRead = Mathf.Clamp(timeToRead - currDuration, MIN_READ_TIME, MAX_READ_TIME);
                // add padding
                timeToRead = (timeToRead / speed) + READ_TIME_PADDING;

                yield return new WaitForSeconds(timeToRead);
            }
            else
            {
                // force complete
                dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
                yield return new WaitForSeconds(0.05f); 
            }

            // move to next line
            OnUserPromptNext();
            yield return null;
        }

        // if there's no more dialogue running
        DisableAuto();

    }

}
