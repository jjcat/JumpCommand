using UnityEngine;
using System;

public class JumpCommandGUI : MonoBehaviour {

    string input = "";
    string inputCopy = "";
    bool   enable = false;
    bool   focus = false;
    int    historyIndex = -1; // -1 means current input
    int    cursorPos = -1;

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
        }
    }

    private void OnSumbitCommandAction() {
        try {
            JumpCommand.Execute(input);
        }
        catch {
          Debug.LogError("Execute Failed ");
          return;
        }
    }


    private void HandleEscape() {
        if (KeyDown("escape") || KeyDown("`")) {
            OnCloseConsoleAction();
            input = "";
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
            string substr = input.Substring(0, te.pos);
            string result = JumpCommand.GetAutoCompletionCommand(substr, input);
            if(result != null) {
                input = result;
                te.selectPos = input.Length;
            }
        }
    }

    private void HandleAutoCompletion()
    {
        // if we hit backspace, don't auto complete
        if (input.Length <= cursorPos)
        {
            cursorPos = input.Length;
            return;
        }

        string AutoCompletion = JumpCommand.GetAutoCompletionCommand(input);
        if (AutoCompletion != null)
        {
            TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            
            if (te != null)
            {
                cursorPos = te.pos = input.Length;  //set cursor position
                te.selectPos = AutoCompletion.Length;  //set selection cursor position
            }

            input = AutoCompletion;
        }
    }

    private void OnCloseConsoleAction() {
        enable = false;
        historyIndex = -1;
    }

    public void Update() {
        if (Input.GetKeyDown(KeyCode.BackQuote)) {
            if(!enable) {
                Invoke("OnOpenConsoleAction",0.1f); // delay 0.1s to fix input text receive backquote
            }
            else {
                enable = false;
                OnCloseConsoleAction();
            }
        }
    }

    public void OnOpenConsoleAction() {   
        enable = true;
        input = "";
        focus = true;
    }


    void OnGUI() {
        if(!enable) return;
        HandleSubmit();
        HandleEscape();
        HandleUpOrDown();
        GUI.SetNextControlName("input");

        String LastInput = input;
        input = GUILayout.TextField(input, GUILayout.Width(Screen.width));

        if (LastInput != input)
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
