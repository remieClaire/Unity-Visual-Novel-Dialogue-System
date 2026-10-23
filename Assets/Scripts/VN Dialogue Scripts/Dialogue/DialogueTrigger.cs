using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] TextAsset inkJSON; // change to private after testing

    void Start()
    {
        DialogueManager.instance.StartStory(inkJSON);
    }
    
}
