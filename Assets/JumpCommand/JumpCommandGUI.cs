using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Text;
using System.Collections.Generic;
using Object = System.Object;

public class JumpCommandGUI : MonoBehaviour {

    string input        = "";
    string inputCopy    = "";
    bool   enable       = false;
    bool   focus        = false;
    int    historyIndex = -1; // -1 means current input
    int    cursorPos    = -1;
    string prompt       = "";
    GameObject lastSelection = null;

    // Use this for initialization
    void Awake () {
        JumpCommand.Awake();
        JumpCommand.OnChangeCallee += HandleChangeCalleeEvent;
    }
    
    private bool KeyDown(string key) {
        return Event.current.Equals(Event.KeyboardEvent(key));
    }

    private void HandleChangeCalleeEvent(Object callee) {
        UpdatePrompt();
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

    private string GetGameObjectFullName(GameObject go) {
        StringBuilder sb = new StringBuilder(128);
        GameObject parent = go;
        List<GameObject> parents = new List<GameObject>();
        while(parent.transform.parent != null) {
            parents.Add(parent);
            parent = parent.transform.parent.gameObject;
        }
        parents.Add(parent);

        for(int i = parents.Count-1; i >= 0; i--) {
            sb.Append("/" + parents[i].name);    
        }
        return sb.ToString();
    }

    // update callee name that will invoke the command
    private void UpdatePrompt() {
        if(JumpCommand.Callee == null) {
            prompt = ">";       
        }
        else if(JumpCommand.Callee is GameObject){
            prompt = GetGameObjectFullName(JumpCommand.Callee as GameObject) + ">";
        }
        else {  // if Callee is not type of GameObject, let it empty now.
            prompt = ">";
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

    [JumpCommandRegister("cd", "Change game object")]
    static private void ChangeGameObjectCallee(string gameObjectPath = "") {
        if(gameObjectPath == "..") {  // return to parent game object
            if(JumpCommand.Callee is GameObject) {
                var go = JumpCommand.Callee as GameObject;
                if(go.transform.parent == null) {
                    JumpCommand.SetCallee(null);    
                }
                else {
                    JumpCommand.SetCallee(go.transform.parent.gameObject); 
                }
            }
        }
        else if(gameObjectPath == "~" || gameObjectPath == "") {  // go to current selected game object
            JumpCommand.SetCallee(Selection.activeGameObject);
        }
        else if(gameObjectPath == "/") {  // go to root, always null
            JumpCommand.SetCallee(null);
        }
        // TODO, parse gameObjectPath and update callee.
    }

    [JumpCommandRegister("sel", "Select callee in the Inspector")]
    static private void SelectCallee() {
        if(JumpCommand.Callee is GameObject) {
            Selection.activeGameObject = (JumpCommand.Callee as GameObject);
        }
    }

    private void OnCloseConsoleAction() {
        enable       = false;
    }

    private void OnSelectionChangedAction() {
        JumpCommand.SetCallee(Selection.activeGameObject);
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

#if UNITY_EDITOR
        // update selected game object as callee.
        if(Selection.activeGameObject != lastSelection) {
            lastSelection = Selection.activeGameObject;
            OnSelectionChangedAction();
        }
#endif
    }

    public void OnOpenConsoleAction() {   
        enable       = true;
        input        = "";
        inputCopy    = "";
        focus        = true;
        cursorPos    = -1;
        historyIndex = -1;
        UpdatePrompt();
        lastSelection = null;
    }

    void OnGUI() {
        if(!enable) return;
        HandleSubmit();
        HandleEscape();
        HandleUpOrDown();
        HandleBackspace();
        GUI.SetNextControlName("input");

        GUILayout.Label(prompt);
        String lastInput = input;
        input = GUILayout.TextField(input,GUILayout.Width(Screen.width));

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
