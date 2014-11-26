using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Text;
using System.Collections.Generic;
using Object = System.Object;

public class JumpCommandGUI : MonoBehaviour {

    string input          = "";
    string inputCopy      = "";
    bool   enable         = false;
    bool   focus          = false;
    int    historyIndex   = -1; // -1 means current input
    int    cursorPos      = -1;
    bool   pressBackspace = false;
    string promptInfo     = "";
    [HideInInspector]
    GUIStyle style = new GUIStyle();  // popup list style
    bool   isPopupListOpen = false;
    string[] popupListContent;
    int    popupListSelPos = 0;
    Vector2 popupListScrollPos = Vector2.zero;
    bool   popupListDisplayUp = true;

    float   uiItemHeight = 25f;
    int     popupListMaxDisplayItemNum  = 8;

    event  Action<string> OnReceiveDownAndUpEvent;
    event  Action         OnReceiveEnterEvent;
    event  Action         OnReceiveEscapeEvent;
    event  Action         OnReceiveBackQuotaEvent;


    GameObject lastSelection = null;
    
    [Tooltip("value between 0 and 1. 0 meas top of screen, 1 means bottom of screen.")]
    [RangeAttribute(0,1f)]
    public float  verticalPos   = 0f;

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
        style.margin.left = style.margin.right = style.margin.top = style.margin.bottom = 0;
        style.fixedHeight = uiItemHeight;
        style.stretchHeight = false;
    }


    
    private bool KeyDown(string key) {
        return Event.current.Equals(Event.KeyboardEvent(key));
    }

    private void HandleChangeCalleeEvent(Object callee) {
        UpdatePrompt();
    }

    private void HandleSubmitPopupList() {
        TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
        input = popupListContent[popupListSelPos].Split(new char[1]{' '})[0];
        te.pos = input.Length;
        te.selectPos = te.pos;
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
        catch (Exception e){
          Debug.LogError("Execute Failed" + e.ToString());
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
            return "/";
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
        return sb.ToString()+"/";
    }

    // update callee name that will invoke the command
    private void UpdatePrompt() {
        promptInfo = GetGameObjectFullName(JumpCommand.Callee as GameObject) + ">";
    }

    private void HandleEscapeInput() {
        // if input text contains contents, just clean off all
        if(input != "") {
            input = "";
        }
        // if input text not contains anything, close console.
        else {
            OnCloseConsoleAction();
        }
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
        pressBackspace = false;
        if(KeyDown("backspace")) {  
            pressBackspace = true;  
            cursorPos = input.Length;
        }
    }

    private void HandleUpOrDownPopupList(string key) {
        int step = -1;
        if(key == "down") {
            step = 1;
        }

        popupListSelPos = Mathf.Clamp(popupListSelPos + step, 0, popupListContent.Length-1);
        float start = popupListScrollPos.y;
        float end   = popupListScrollPos.y + (popupListMaxDisplayItemNum * uiItemHeight-1);
        if( (popupListSelPos * uiItemHeight) < start && key == "up") {
            popupListScrollPos = new Vector2(popupListScrollPos.x, Mathf.Clamp(popupListScrollPos.y-uiItemHeight, 0, (popupListContent.Length-1)*uiItemHeight ));
        }
        else if( (popupListSelPos * uiItemHeight) > end && key == "down") {
            popupListScrollPos = new Vector2(popupListScrollPos.x, Mathf.Clamp(popupListScrollPos.y+uiItemHeight, 0, (popupListContent.Length-1)*uiItemHeight ));
        }
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
        if(KeyDown("tab") && isPopupListOpen) {
            input = popupListContent[popupListSelPos].Split(new char[1]{' '})[0];
            te.pos = input.Length;
            te.selectPos = te.pos;
            ClosePopupList();
        }
        else if(KeyDown("tab") && !isPopupListOpen){
            List<string> autoCompletions = JumpCommand.GetAutoCompletionCommands(input);
            if(autoCompletions.Count > 0 ) {
                popupListContent = autoCompletions.ToArray();
                input = popupListContent[popupListSelPos].Split(new char[1]{' '})[0];
                te.pos = input.Length;
                te.selectPos = te.pos;
                OpenPopupList();
            }        
        }
        // Press tab to switch among auto-completion commands

        // if (KeyDown("tab"))
        // {
        //     // eg: com[mand]
        //     // The unselected part of a command
        //     string substr  = input.Substring(0, te.pos);
        //     string command = JumpCommand.GetAutoCompletionCommand(substr, input);
        //     if(command != null) {
        //         input = command;
        //         te.pos = input.Length;
        //     }
        // }
    }


    private void ShouldOpemPopupList() {
        List<string> autoCompletions = JumpCommand.GetAutoCompletionCommands(input);
        if(autoCompletions.Count > 0 ) {
            popupListContent = autoCompletions.ToArray();
            if(!isPopupListOpen) {
                OpenPopupList();
            }
        }        
        else {
            if(isPopupListOpen) {
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
        if(gameObjectPath == "~" || gameObjectPath == "") {  // go to current selected game object
            JumpCommand.SetCallee(Selection.activeGameObject);
        }
        else if(gameObjectPath == "/") {  // go to root, always null
            JumpCommand.SetCallee(null);
        }
        else if(gameObjectPath.StartsWith("/")) { // use absolut path
            var foundOne = GameObject.Find(gameObjectPath);
            if(foundOne != null) {
                JumpCommand.SetCallee(foundOne);
            }
            else {
                Debug.LogError("Can not found " + gameObjectPath);
            }
        }
        else {
            string[] paths = gameObjectPath.Split(new char[]{'/'});
            foreach(var p in paths) {
                ChangeGameObjectCalleeInner(p);
            }            
        }
    }

    static private void ChangeGameObjectCalleeInner(string gameObjectPath = "") {
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
            string fullGameObjectPath = GetGameObjectFullName(JumpCommand.Callee as GameObject) + gameObjectPath;
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
        isPopupListOpen = false;
        OnReceiveDownAndUpEvent += HandleUpOrDownInput;
        OnReceiveEnterEvent     += HandleSubmitInput;
        OnReceiveEscapeEvent    += HandleEscapeInput;
        OnReceiveBackQuotaEvent += HandleBackQuotaInput;
    }

    void OpenPopupList() {  
        isPopupListOpen = true;
        OnReceiveDownAndUpEvent -= HandleUpOrDownInput;
        OnReceiveEscapeEvent    -= HandleEscapeInput;
        OnReceiveBackQuotaEvent -= HandleBackQuotaInput;
        OnReceiveEnterEvent     -= HandleSubmitInput;
        OnReceiveDownAndUpEvent += HandleUpOrDownPopupList;
        OnReceiveEscapeEvent    += HandleEscapePopupList;
        OnReceiveBackQuotaEvent += HandleBackQuotaPopupList;
        OnReceiveEnterEvent     += HandleSubmitPopupList;
        popupListSelPos = 0;
        popupListScrollPos = Vector2.zero;
    }

    void ClosePopupList() {
        isPopupListOpen = false; 
        OnReceiveDownAndUpEvent += HandleUpOrDownInput;
        OnReceiveEscapeEvent    += HandleEscapeInput;
        OnReceiveBackQuotaEvent += HandleBackQuotaInput;
        OnReceiveEnterEvent     += HandleSubmitInput;
        OnReceiveDownAndUpEvent -= HandleUpOrDownPopupList;
        OnReceiveEscapeEvent    -= HandleEscapePopupList;
        OnReceiveBackQuotaEvent -= HandleBackQuotaPopupList;
        OnReceiveEnterEvent     -= HandleSubmitPopupList;
        popupListSelPos = 0;
        popupListScrollPos = Vector2.zero;
    }

    void HandleDragExited() {
        Event currentEvent = Event.current;
        var rect = new Rect(0, Screen.height, Screen.width, Screen.height);
        if(currentEvent.type == EventType.DragExited ) {
            if(DragAndDrop.objectReferences.Length == 1) {
                GameObject dragObj = DragAndDrop.objectReferences[0] as GameObject;
                if(dragObj != null) {
                    string path = "\"" + GetGameObjectFullName(dragObj) + "\"";
                    TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    if (te != null) {
                        te.DeleteSelection();
                        input = input.Insert(te.pos, path);
                        te.pos = te.selectPos = te.pos + path.Length;
                    }

                }
            }
        }        
    }

    bool isDragging = false;
    bool isPressGUI = false;
    Vector2 startMousePos = Vector2.zero;
    float startVerticalPos = 0;

    void HandleMouseDrag() {
        Event currentEvent = Event.current;
        Vector2 mousePos = currentEvent.mousePosition;
        if(currentEvent.type == EventType.MouseDrag) {
            if(isPressGUI && isDragging) {
                float delta = (mousePos - startMousePos).y/(Screen.height - uiItemHeight*2);
                verticalPos = Mathf.Clamp(startVerticalPos + delta, 0, 1f) ;
                Debug.Log("Update Position");
            }
            else if(!isDragging && isPressGUI) {
                Debug.Log("Start Drag");
                isDragging = true;
            }
        }
        else if(currentEvent.type == EventType.MouseUp) {
            if(isPressGUI && isDragging) {
                isPressGUI = false;
                isDragging = false;
                Debug.Log("Over Drag");
            }
        }
        else if(currentEvent.type == EventType.MouseDown) {
            float yPos = (Screen.height - uiItemHeight*2 )*verticalPos;
            if( new Rect(0, yPos, Screen.width, uiItemHeight*2).Contains(mousePos)) {
                Debug.Log("Hit GUI");
                startMousePos = mousePos;
                startVerticalPos = verticalPos;
                isPressGUI = true;
            }
        }
    }

    void OnGUI() {
        if(!enable) return;
        DispatchSubmitEvent();
        DispatchEscapeEvent();
        DispatchUpOrDownEvent();
        DispatchBackQuotaEvent();
        HandleBackspace();
        HandleDragExited();
        HandleMouseDrag();

        float yPos = (Screen.height - uiItemHeight*2 )*verticalPos;
        GUI.SetNextControlName("input");
        GUI.Label(new Rect(0, yPos, Screen.width, uiItemHeight), promptInfo);
        String lastInput = input;
        yPos += uiItemHeight;
        input = GUI.TextField(new Rect(0, yPos, Screen.width, uiItemHeight), input);
        yPos += uiItemHeight;

        // draw popup list if open
        if(isPopupListOpen && popupListDisplayUp ) {
            float scrollWidth= 16f;

            float popupListHeight = popupListMaxDisplayItemNum * uiItemHeight;
            float contentHeight = uiItemHeight * popupListContent.Length;

            // let draw above screen if yPos is greater than half screen height, otherwise popup list will draw out of screen.
            if(yPos > Screen.height/2) {
                yPos = (Screen.height - uiItemHeight*2)*verticalPos - Mathf.Min(popupListHeight,contentHeight);
            }

            popupListScrollPos = GUI.BeginScrollView(new Rect(0, yPos, Screen.width, popupListHeight), popupListScrollPos, new Rect(0,0,Screen.width-scrollWidth, contentHeight),false, false);
                popupListSelPos = GUI.SelectionGrid(new Rect(0, 0, Screen.width, contentHeight), popupListSelPos, popupListContent, 1, style);
            GUI.EndScrollView();        
        }
        
        if (lastInput != input && !pressBackspace) {
            ShouldOpemPopupList();
            //HandleAutoCompletion();
        }
        HandleTab();

        if (focus) {
            GUI.FocusControl("input");
            focus = false;
            input = "";
        }

        if(input == "" && isPopupListOpen && pressBackspace) {
            ClosePopupList();
        }
    }

    int InputStatParameterCount {
        get {
            return JumpCommand.ParseArguments(input).Length;
        }
    }


    string[] GetAllGameObjectFullName(GameObject parent) {
        List<string> result = new List<string>();
        var parentPath = GetGameObjectFullName(parent);
        if(parent == null) {
            foreach (GameObject go in UnityEngine.Object.FindObjectsOfType(typeof(GameObject))) {
                if (go.transform.parent == null) {
                    var path = GetGameObjectFullName(go);
                    path.Substring(1);
                    result.Add(path);
                }
            }
        }
        else {
            for(int i = 0; i < parent.transform.childCount; i++) {
                var path = GetGameObjectFullName(parent.transform.GetChild(i).gameObject);
                path = path.Substring(parentPath.Length); 
                result.Add(path);
            }            
        }
        return result.ToArray();
    }

    // void OpenPopupListForGameObject() {
    //     isPopupListOpen = true;
    //     popupListContent = GetAllGameObjectFullName(JumpCommand.Callee as GameObject);
    // }

    // void ClosePopupListForGameObject() {
    //     isPopupListOpen = false;
    // }

    // string InputStatCurrentParameter {
    //     get {

    //     }
    // }
}
