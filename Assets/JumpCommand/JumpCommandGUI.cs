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
    [HideInInspector]
    GUIStyle style = new GUIStyle();  // popup list style
    bool   enablePopupList = false;
    string[] popupListContent = {"1","2","3","4"};
    int    popupListSelected = 0;
    Vector2 popupListScrollPos = Vector2.zero;

    event  Action<string> OnReceiveDownAndUpEvent;
    event  Action         OnReceiveEnterEvent;
    event  Action         OnReceiveEscapeEvent;
    event  Action         OnReceiveBackQuotaEvent;


    GameObject lastSelection = null;
    
    [Tooltip("value between 0 and 1. 0 meas top of screen, 1 means bottom of screen.")]
    [RangeAttribute(0,1f)]
    public float  yPos   = 0f;

    // Use this for initialization
    void Awake () {
        JumpCommand.Awake();
        JumpCommand.OnChangeCallee += HandleChangeCalleeEvent;
        CreatePopupListStyle();
    }

    void CreatePopupListStyle() {
        style.alignment = TextAnchor.MiddleLeft;
        style.normal.textColor = Color.white;
        var tex = new Texture2D(2, 2);
        var colors = new Color[4]{Color.white,Color.white,Color.white,Color.white};
        var bg = new Texture2D(2, 2);
        var bgColor = new Color(0,0,0,0.5f);
        var bgColors = new Color[4]{bgColor,bgColor,bgColor,bgColor};
        tex.SetPixels(colors);
        bg.SetPixels(bgColors);
        tex.Apply();
        bg.Apply();
        style.normal.background = bg;
        style.onNormal.background = tex;        
        style.padding.left = style.padding.right = style.padding.top = style.padding.bottom = 4;        
    }


    
    private bool KeyDown(string key) {
        return Event.current.Equals(Event.KeyboardEvent(key));
    }

    private void HandleChangeCalleeEvent(Object callee) {
        UpdatePrompt();
    }

    private void HandleSubmitPopupList() {
        input = popupListContent[popupListSelected].Split(new char[1]{' '})[0];
        ClosePopupList();         
    }

    private void HandleSubmitInput() {
        OnSumbitCommandAction();
        input = "";
        historyIndex = -1;
        cursorPos    = -1;
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

    [JumpCommandRegister("ls", "List children of callee, if callee is null list all root game objects")]
    static private void ListChildren() {
        if(JumpCommand.Callee is GameObject) {
            var go = JumpCommand.Callee as GameObject;
            for(int i = 0; i < go.transform.childCount; i++) {
                Debug.Log(go.transform.GetChild(i).name);
            }
        }
        else if(JumpCommand.Callee == null){
            foreach (GameObject go in UnityEngine.Object.FindObjectsOfType(typeof(GameObject))) {
                if (go.transform.parent == null) {
                    Debug.Log(go.name);
                }
            }
        }
    }

    static private string GetGameObjectFullName(GameObject go) {
        if(go == null) {
            return "";
        }

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

    private void HandleEscapeInput() {
        OnCloseConsoleAction();
    }

    private void HandleBackQuotaInput() {
        OnCloseConsoleAction();        
    }

    private void HandleBackQuotaPopupList() {
        OnCloseConsoleAction();        
    }

    private void HandleEscapePopupList() {
        ClosePopupList();
    }

    // fix cursorPos is not correct if pressing backspace.
    private void HandleBackspace() {
        if(KeyDown("backspace")) {    
            cursorPos = input.Length;
        }
    }

    private void HandleUpOrDownPopupList(string key) {
        int step = -1;
        if(key == "down") {
            step = 1;
        }

        popupListSelected = Mathf.Clamp(popupListSelected + step, 0, popupListContent.Length-1);
    }

    private void HandleUpOrDownInput(string key) {
        int step = 1;
        if(key == "down") {
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

    private void DispatchSubmitEvent() {
        if (KeyDown("[enter]") || KeyDown("return")) {
            if(OnReceiveEnterEvent != null) {
                OnReceiveEnterEvent();
            }
        }
    }

    private void DispatchUpOrDownEvent() {
        if(KeyDown("down")) {
            if(OnReceiveDownAndUpEvent != null) {
                OnReceiveDownAndUpEvent("down");
            }
        }
        else if(KeyDown("up")) {
            if(OnReceiveDownAndUpEvent != null) {
                OnReceiveDownAndUpEvent("up");
            }
        }
    }

    private void DispatchBackQuotaEvent() {
        if(KeyDown("`")) {
            if (OnReceiveBackQuotaEvent!= null){
                OnReceiveBackQuotaEvent();
            }            
        }
    }

    private void DispatchEscapeEvent() {
        if (KeyDown("escape") ) {
            if (OnReceiveEscapeEvent!= null){
                OnReceiveEscapeEvent();
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


    private void ShouldOpemPopupList() {
        List<string> autoCompletions = JumpCommand.GetAutoCompletionCommands(input);
        if(autoCompletions.Count > 0 ) {
            popupListContent = autoCompletions.ToArray();
            if(!enablePopupList) {
                OpenPopupList();
            }
        }        
        else {
            if(enablePopupList) {
                ClosePopupList();
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
        else {
            string fullGameObjectPath = GetGameObjectFullName(JumpCommand.Callee as GameObject) +"/"+ gameObjectPath;
            var foundOne = GameObject.Find(fullGameObjectPath);
            if(foundOne != null) {
                JumpCommand.SetCallee(foundOne);
            }
            else {
                Debug.LogError("Can not found " + gameObjectPath);
            }
        }
    }

    [JumpCommandRegister("sel", "Select callee in the Inspector")]
    static private void SelectCallee() {
        if(JumpCommand.Callee is GameObject) {
            Selection.activeGameObject = (JumpCommand.Callee as GameObject);
        }
    }

    private void OnCloseConsoleAction() {
        enable       = false;
        OnReceiveDownAndUpEvent -= HandleUpOrDownInput;
        OnReceiveEnterEvent     -= HandleSubmitInput;
        OnReceiveEscapeEvent    -= HandleEscapeInput;
        OnReceiveBackQuotaEvent -= HandleBackQuotaInput;
        OnReceiveDownAndUpEvent -= HandleUpOrDownPopupList;
        OnReceiveEnterEvent     -= HandleSubmitPopupList;
        OnReceiveEscapeEvent    -= HandleEscapePopupList;
        OnReceiveBackQuotaEvent -= HandleBackQuotaPopupList;

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
        enablePopupList = false;
        OnReceiveDownAndUpEvent += HandleUpOrDownInput;
        OnReceiveEnterEvent     += HandleSubmitInput;
        OnReceiveEscapeEvent    += HandleEscapeInput;
        OnReceiveBackQuotaEvent += HandleBackQuotaInput;
    }

    void OpenPopupList() {  
        enablePopupList = true;
        OnReceiveDownAndUpEvent -= HandleUpOrDownInput;
        OnReceiveEscapeEvent    -= HandleEscapeInput;
        OnReceiveBackQuotaEvent -= HandleBackQuotaInput;
        OnReceiveEnterEvent     -= HandleSubmitInput;
        OnReceiveDownAndUpEvent += HandleUpOrDownPopupList;
        OnReceiveEscapeEvent    += HandleEscapePopupList;
        OnReceiveBackQuotaEvent += HandleBackQuotaPopupList;
        OnReceiveEnterEvent     += HandleSubmitPopupList;
        popupListSelected = 0;
    }

    void ClosePopupList() {
        enablePopupList = false; 
        OnReceiveDownAndUpEvent += HandleUpOrDownInput;
        OnReceiveEscapeEvent    += HandleEscapeInput;
        OnReceiveBackQuotaEvent += HandleBackQuotaInput;
        OnReceiveEnterEvent     += HandleSubmitInput;
        OnReceiveDownAndUpEvent -= HandleUpOrDownPopupList;
        OnReceiveEscapeEvent    -= HandleEscapePopupList;
        OnReceiveBackQuotaEvent -= HandleBackQuotaPopupList;
        OnReceiveEnterEvent     -= HandleSubmitPopupList;
    }

    void OnGUI() {
        if(!enable) return;
        DispatchSubmitEvent();
        DispatchEscapeEvent();
        DispatchUpOrDownEvent();
        DispatchBackQuotaEvent();
        HandleBackspace();
        GUI.SetNextControlName("input");

        GUILayout.BeginArea(new Rect(0, (Screen.height - 50)*yPos, Screen.width, Screen.height));
        
        GUILayout.Label(prompt);
        String lastInput = input;
        input = GUILayout.TextField(input,GUILayout.Width(Screen.width));
        if(enablePopupList) {
            popupListScrollPos = GUILayout.BeginScrollView(popupListScrollPos, false, false, GUILayout.MaxHeight(250));
            popupListSelected = GUILayout.SelectionGrid(popupListSelected, popupListContent, 1, style, GUILayout.Width(Screen.width));
            GUILayout.EndScrollView();
        }
        GUILayout.EndArea();
        if (lastInput != input )
        {
            ShouldOpemPopupList();
            //HandleAutoCompletion();
        }
        HandleTab();

        if (focus) {
            GUI.FocusControl("input");
            focus = false;
            input = "";
        }

    }
}
