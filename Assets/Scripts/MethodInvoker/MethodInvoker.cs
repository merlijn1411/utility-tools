using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

[System.Serializable]
public class MethodCall
{
    public GameObject targetObject;
    public string componentName;
    public string methodName;
    [HideInInspector] public List<string> componentNames = new List<string>();
    [HideInInspector] public List<string> methodNames = new List<string>();
    [HideInInspector] public string parameterType;
    public int intParameter;
    public float floatParameter;
    public string stringParameter;
    public bool boolParameter;
    public UnityEngine.Object objectParameter;
}

public class MethodInvoker : MonoBehaviour
{
    public List<MethodCall> methodCalls = new List<MethodCall>();
    [Tooltip("NOTE: The interval feature works only if you start the scene")]public float interval;
    public void InvokeMethod(MethodCall call)
    {
            var component = call.targetObject.GetComponents<Component>()
                .FirstOrDefault(c => c.GetType().Name == call.componentName);

            MethodInfo methodInfo = component.GetType().GetMethod(
                call.methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
            );

            try
            {
                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (parameters.Length == 0)
                {
                    methodInfo.Invoke(component, null);
                }
                else if (parameters.Length == 1)
                {
                    object parameterValue = GetParameterValue(call, parameters[0].ParameterType);
                    if (parameterValue != null)
                    {
                        methodInfo.Invoke(component, new object[] { parameterValue });
                    }
                }
                else
                {
                    Debug.LogError($"Methode {call.methodName} to much parameters!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Mistake occured by calling: {call.methodName}: {ex.Message}");
            }
    }

    public void StartInvoking()
    {
        StartCoroutine(InvokeAllMethods());
    }

    private IEnumerator InvokeAllMethods()
    {
        foreach (var call in methodCalls)
        {
            InvokeMethod(call);
            yield return new WaitForSeconds(interval);
        }
    }
    
    

    private object GetParameterValue(MethodCall call, Type paramType)
    {
        if (paramType == typeof(int)) return call.intParameter;
        if (paramType == typeof(float)) return call.floatParameter;
        if (paramType == typeof(string)) return call.stringParameter;
        if (paramType == typeof(bool)) return call.boolParameter;
        if (typeof(UnityEngine.Object).IsAssignableFrom(paramType)) return call.objectParameter;
        return null;
    }

    public void UpdateComponents(MethodCall call)
    {
        call.componentNames.Clear();
        call.componentName = "";
        call.methodNames.Clear();
        call.methodName = "";
        call.parameterType = "";

        if (call.targetObject != null)
        {
            Component[] components = call.targetObject.GetComponents<Component>();
            call.componentNames = components.Select(c => c.GetType().Name).ToList();
        }
    }

    public void UpdateMethods(MethodCall call)
    {
        call.methodNames.Clear();
        call.parameterType = ""; // Reset parameter type, maar behoud methodName indien mogelijk

        if (call.targetObject == null || string.IsNullOrEmpty(call.componentName))
        {
            Debug.LogWarning($"UpdateMethods: Geen GameObject of component geselecteerd (targetObject: {call.targetObject}, componentName: {call.componentName})");
            return;
        }

        Component component = call.targetObject.GetComponents<Component>()
            .FirstOrDefault(c => c.GetType().Name == call.componentName);

        if (component == null)
        {
            Debug.LogWarning($"UpdateMethods: Component {call.componentName} niet gevonden op GameObject {call.targetObject.name}");
            return;
        }

        MethodInfo[] methods = component.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetParameters().Length <= 1)
            .ToArray();

        call.methodNames = methods.Select(m => m.Name).ToList();

        // Controleer of de huidige methodName nog geldig is
        if (!string.IsNullOrEmpty(call.methodName) && !call.methodNames.Contains(call.methodName))
        {
            Debug.LogWarning($"UpdateMethods: Huidige methode {call.methodName} niet gevonden in nieuwe lijst, resetten.");
            call.methodName = "";
            call.parameterType = "";
        }

        // Update parameter type als er een geldige methode geselecteerd is
        if (!string.IsNullOrEmpty(call.methodName))
        {
            MethodInfo selectedMethod = methods.FirstOrDefault(m => m.Name == call.methodName);
            if (selectedMethod != null)
            {
                call.parameterType = selectedMethod.GetParameters().Length == 1 ? selectedMethod.GetParameters()[0].ParameterType.Name : "";
            }
        }
    }
}