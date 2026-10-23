using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        InputAction leftClickAction = InputSystem.actions.FindAction("Click");
        InputAction submitAction = InputSystem.actions.FindAction("Submit");

        bool userPrompt = leftClickAction.WasPressedThisFrame() || submitAction.WasPressedThisFrame();

        if (userPrompt)
            DialogueAdvance();
    }

    // ---Dialogue Input Functions---
    public void DialogueAdvance()
    {
        DialogueSystem.instance.OnUserPromptNext();
    }

    
}
