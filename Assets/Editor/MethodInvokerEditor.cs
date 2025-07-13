using UnityEditor;
using UnityEngine;
using System;

[CustomEditor(typeof(MethodInvoker))]
public class MethodInvokerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MethodInvoker invoker = (MethodInvoker)target;
        serializedObject.Update();
        EditorGUIUtility.labelWidth = 135f;

        // Lijst van methoden
        SerializedProperty methodCallsProp = serializedObject.FindProperty("methodCalls");
        
        EditorGUILayout.LabelField("Method Calls", EditorStyles.boldLabel);
        
        for (var i = 0; i < methodCallsProp.arraySize; i++)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            SerializedProperty callProp = methodCallsProp.GetArrayElementAtIndex(i);
            
            SerializedProperty targetObjectProp = callProp.FindPropertyRelative("targetObject");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(targetObjectProp, new GUIContent($"Target Object {i + 1}"));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(invoker, "Change Target Object");
                callProp.FindPropertyRelative("componentName").stringValue = "";
                callProp.FindPropertyRelative("methodName").stringValue = "";
                callProp.FindPropertyRelative("parameterType").stringValue = "";
                invoker.UpdateComponents(invoker.methodCalls[i]);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(invoker);
                Repaint();
            }

            // Component dropdown
            EditorGUILayout.BeginHorizontal();
            
            MethodCall callData = invoker.methodCalls[i];
            if (callData.componentNames.Count == 0)
            {
                invoker.UpdateComponents(callData);
                serializedObject.ApplyModifiedProperties();
            }

            SerializedProperty componentNameProp = callProp.FindPropertyRelative("componentName");
            if (callData.componentNames.Count > 0)
            {
                EditorGUI.BeginChangeCheck();
                int currentComponentIndex = string.IsNullOrEmpty(componentNameProp.stringValue)
                    ? 0
                    : callData.componentNames.IndexOf(componentNameProp.stringValue);
                if (currentComponentIndex < 0) currentComponentIndex = 0;
                int newComponentIndex = EditorGUILayout.Popup("", currentComponentIndex, callData.componentNames.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(invoker, "Change Selected Component");
                    componentNameProp.stringValue = callData.componentNames[newComponentIndex];
                    callProp.FindPropertyRelative("methodName").stringValue = "";
                    callProp.FindPropertyRelative("parameterType").stringValue = "";
                    invoker.UpdateMethods(callData);
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(invoker);
                    Repaint();
                }
            }
            // Methode dropdown
            if (!string.IsNullOrEmpty(componentNameProp.stringValue))
            {
                invoker.UpdateMethods(callData);
                serializedObject.ApplyModifiedProperties();

                SerializedProperty methodNameProp = callProp.FindPropertyRelative("methodName");
                if (callData.methodNames.Count > 0)
                {
                    EditorGUI.BeginChangeCheck();
                    int currentMethodIndex = string.IsNullOrEmpty(methodNameProp.stringValue)
                        ? 0
                        : callData.methodNames.IndexOf(methodNameProp.stringValue);
                    if (currentMethodIndex < 0) currentMethodIndex = 0;
                    int newMethodIndex = EditorGUILayout.Popup("", currentMethodIndex, callData.methodNames.ToArray());
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(invoker, "Change Selected Method");
                        methodNameProp.stringValue = callData.methodNames[newMethodIndex];
                        callProp.FindPropertyRelative("parameterType").stringValue = "";
                        invoker.UpdateMethods(callData); // Update parameter type
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(invoker);
                        Repaint();
                    }
                }
                else
                {
                    Debug.LogWarning($"Geen methoden beschikbaar voor component {componentNameProp.stringValue} op GameObject {callData.targetObject?.name}.");
                }
            }
            
            // Parameter field (Include)
            SerializedProperty parameterTypeProp = callProp.FindPropertyRelative("parameterType");
            if (!string.IsNullOrEmpty(parameterTypeProp.stringValue))
            {
                EditorGUI.indentLevel++;

                void HandleChange<T>(string label, T currentValue, Action<T> onChange, Func<string, T, T> drawField)
                {
                    EditorGUI.BeginChangeCheck();
                    T newValue = drawField(label,currentValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(invoker, $"Change {typeof(T).Name} Parameter");
                        onChange(newValue);
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(invoker);
                        Repaint();
                    }
                }



                switch (parameterTypeProp.stringValue)
                {
                    case "Int32":
                        SerializedProperty intParamProp = callProp.FindPropertyRelative("intParameter");
                        HandleChange("", intParamProp.intValue, v => intParamProp.intValue = v, 
                            (label, value) => EditorGUILayout.IntField(value));
                        break;
                    case "Single":
                        SerializedProperty floatParamProp = callProp.FindPropertyRelative("floatParameter");
                        HandleChange("", floatParamProp.floatValue, v => floatParamProp.floatValue = v, 
                            (label, value) => EditorGUILayout.FloatField(value));
                        break;
                    case "String":
                        SerializedProperty stringParamProp = callProp.FindPropertyRelative("stringParameter");
                        HandleChange("", stringParamProp.stringValue, v => stringParamProp.stringValue = v, 
                            (label, value) => EditorGUILayout.TextField(value));
                        break;
                    case "Boolean":
                        SerializedProperty boolParamProp = callProp.FindPropertyRelative("boolParameter");
                        HandleChange("", boolParamProp.boolValue, v => boolParamProp.boolValue = v, 
                            (label, value) => EditorGUILayout.Toggle(value));
                        break;
                    default:
                        SerializedProperty objectParamProp = callProp.FindPropertyRelative("objectParameter");
                        HandleChange("", objectParamProp.objectReferenceValue, v => objectParamProp.objectReferenceValue = v,
                            (label, value) => EditorGUILayout.ObjectField(value, typeof(UnityEngine.Object), true));
                        break;
                }
                

                EditorGUI.indentLevel--;
            }
           
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Invoke Method"))
            {
                Undo.RecordObject(invoker, "Invoke Single Method");
                invoker.InvokeMethod(callData);
                EditorUtility.SetDirty(invoker);
                Repaint();
            }
            
            if (GUILayout.Button("Remove Method"))
            {
                Undo.RecordObject(invoker, "Remove Method Call");
                methodCallsProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(invoker);
                Repaint();
                break;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Add Method"))
        {
            Undo.RecordObject(invoker, "Add Method Call");
            methodCallsProp.arraySize++;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(invoker);
            Repaint();
        }
        
        if (methodCallsProp.arraySize > 0 && GUILayout.Button("Invoke All Methods"))
        {
            invoker.StartInvoking();
        }
        EditorGUILayout.EndHorizontal();

        var intervalProp = serializedObject.FindProperty("interval");
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(intervalProp, new GUIContent("Interval"));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(invoker, "Change Interval");
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(invoker);
            Repaint();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}