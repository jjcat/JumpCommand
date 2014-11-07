using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
using Object = System.Object;


public class JumpCommandObject  {
    public string      Command{get;private set;}
    public MethodInfo  Method {get;private set;}
    public string      Help   {get;private set;}
    public Type Type {
        get {
            return Method.DeclaringType;
        }
    }

    public JumpCommandObject(string command, MethodInfo method, string help) {
        Command = command;
        Method  = method;
        Help    = help;
    }

    public bool Call(string[] paramStr, Object callee = null) {        
        // create parameter list
        ParameterInfo[] paramInfoLst = Method.GetParameters();
        Object[] paramLst = new Object[paramInfoLst.Length];
        if(paramInfoLst.Length != paramStr.Length) {
            Debug.LogError(string.Format("{0} need {1} parameters, but received {2} parameters", Method.Name, paramInfoLst.Length, paramStr.Length));
            return false;
        }
        
        for (int i = 0; i < paramInfoLst.Length; ++i) {
            paramLst[i] = TypeDescriptor.GetConverter(paramInfoLst[i].ParameterType).ConvertFrom(paramStr[i]);
        }

        // call methord with parameter list
        if(Method.IsStatic) {
            Method.Invoke(null, paramLst);
        }
        else {
            if(callee == null) {
                Debug.LogError(string.Format("Need type of \"{0}\" instance object to execute", Type.Name));
                return false;
            }
            else {
                Method.Invoke(callee, paramLst);
            }
        }
        return true;
    }

}
