using System;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
using Object = System.Object;


public class JumpCommandObject  {
    public string      Command {get;private set;}
    public MethodInfo  Method  {get;private set;}
    public string      Help    {get;private set;}
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

    public override string ToString() {
        string parminfo = ParametersInfo();
        if(parminfo!="") {
            parminfo =  parminfo;
        }
        string helpinfo = Help;
        if(helpinfo != "") {
            helpinfo = "\"" + helpinfo + "\"";
        }
        return string.Format("{0,-10}{1} {2}", Command, parminfo, helpinfo);
    }

    public string ParametersInfo() {
        string result = "";
        var paramInfoLst = Method.GetParameters();
        for (int i = 0; i < paramInfoLst.Length-1; ++i) {
            result += paramInfoLst[i].ParameterType.Name + " ";
        }
        if(paramInfoLst.Length > 0) {
            result += paramInfoLst[paramInfoLst.Length-1].ParameterType.Name;
        }
        return result;
    }


    public bool Call(string[] paramStr, Object callee = null) {        
        // create parameter list
        ParameterInfo[] paramInfoLst = Method.GetParameters();
        Object[] paramLst = new Object[paramInfoLst.Length];
        
        for (int i = 0; i < paramInfoLst.Length; ++i) {
            if(i >= paramStr.Length) { // if param string item number is less than param info item number, check defalut value from param info. 
                if(paramInfoLst[i].RawDefaultValue == null) {
                   Debug.LogError(string.Format("Missing argument '#{0}' when call method {1}", i, Method.Name));
                   return false;
                }
                else {
                    paramLst[i] = paramInfoLst[i].RawDefaultValue;    
                }
            }
            else {
                paramLst[i] = TypeDescriptor.GetConverter(paramInfoLst[i].ParameterType).ConvertFrom(paramStr[i]);
            }
        }

        // call method with parameter list
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
