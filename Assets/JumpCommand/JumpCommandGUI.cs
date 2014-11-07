using UnityEngine;
using System;

public class JumpCommandGUI : MonoBehaviour {

	string input = "";
	bool   enable = false;
	bool   focus = false;

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
        }
    }

    private void OnSumbitCommandAction() {
	    try {
	        JumpCommand.Execute(input);
	    }
	    catch {
	      Debug.LogError("Execute Faield ");
	      return;
	    }
	}


    private void HandleEscape() {
        if (KeyDown("escape") || KeyDown("`")) {
            OnCloseConsoleAction();
            input = "";
        }
    }

    private void OnCloseConsoleAction() {
        enable = false;
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
		GUI.SetNextControlName("input");
        input = GUILayout.TextField(input, GUILayout.Width(Screen.width));
        if (focus) {
            GUI.FocusControl("input");
            focus = false;
            input = "";
        }
	}
}
