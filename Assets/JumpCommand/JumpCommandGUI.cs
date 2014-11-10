using UnityEngine;
using System;

public class JumpCommandGUI : MonoBehaviour {

    string input        = "";
    string inputCopy    = "";
    bool   enable       = false;
    bool   focus        = false;
    int    historyIndex = -1; // -1 means current input
    int    cursorPos    = -1;

    // Use this for initialization
    void Awake () {
        JumpCommand.Awake();
    }
    
    private bool KeyDown(string key) {
        return Event.current.Equals(Event.KeyboardEvent(key));
    }


    private void HandleSubmit() {
        if (KeyDown("[enter]") || KeyDown("return")) {
            OnSumbitCommandAction();
            input = "";
            historyIndex = -1;
            cursorPos    = -1;
        }
    }

    private void OnSumbitCommandAction() {
        try {
            JumpCommand.Execute(input);
        }
        catch {
          Debug.LogError("Execute Failed");
          return;
        }
    }


    private void HandleEscape() {
        if (KeyDown("escape") || KeyDown("`")) {
            OnCloseConsoleAction();
        }
    }

    // fix cursorPos is not correct if pressing backspace.
    private void HandleBackspace() {
        if(KeyDown("backspace")) {    
            cursorPos = input.Length;
        }
    }

    private void HandleUpOrDown() {
        if(KeyDown("up") || KeyDown("down")) {
            int step = 1;
            if(KeyDown("down")) {
                step = -1;
            }

            if(historyIndex == -1) {
                inputCopy = input;  // save current input
            }

            historyIndex = Mathf.Clamp(historyIndex+step, -1, JumpCommand.HistoryNum - 1);
            if(historyIndex == -1) {
                input = inputCopy;
            }
            else {
                input = JumpCommand.GetHistory(historyIndex);
            }
        }
    }

    private void HandleTab()
    {
        TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);

        // Press tab to switch among auto-completion commands
        if (KeyDown("tab"))
        {
            // eg: com[mand]
            // The unselected part of a command
            string substr  = input.Substring(0, te.pos);
            string command = JumpCommand.GetAutoCompletionCommand(substr, input);
            if(command != null) {
                input = command;
                te.selectPos = input.Length;
            }
        }
    }

    private void HandleAutoCompletion() {
        // if we hit backspace, don't auto complete
        if (input.Length <= cursorPos) {
            cursorPos = input.Length;
            return;
        }

        string autoCompletion = JumpCommand.GetAutoCompletionCommand(input);
        if (autoCompletion != null) {
            TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            
            if (te != null) {
                cursorPos = te.pos = input.Length;  //set cursor position
                te.selectPos = autoCompletion.Length;  //set selection cursor position
            }

            input = autoCompletion;
        }
    }

    private void OnCloseConsoleAction() {
        enable       = false;
    }

    public void Update() {
        if (Input.GetKeyDown(KeyCode.BackQuote)) {
            if(!enable) {
                Invoke("OnOpenConsoleAction", 0.1f); // delay 0.1s to fix input text receive backquote
            }
            else {
                OnCloseConsoleAction();
            }
        }
    }

    public void OnOpenConsoleAction() {   
        enable       = true;
        input        = "";
        inputCopy    = "";
        focus        = true;
        cursorPos    = -1;
        historyIndex = -1;
    }


    void OnGUI() {
        if(!enable) return;
        HandleSubmit();
        HandleEscape();
        HandleUpOrDown();
        HandleBackspace();
        GUI.SetNextControlName("input");

        String lastInput = input;
        input = GUILayout.TextField(input, GUILayout.Width(Screen.width));

        if (lastInput != input ) 
        {
            HandleAutoCompletion();
        }
        HandleTab();

        if (focus) {
            GUI.FocusControl("input");
            focus = false;
            input = "";
        }
    }
}
