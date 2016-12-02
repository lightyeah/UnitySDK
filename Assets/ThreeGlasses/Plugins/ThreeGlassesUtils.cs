﻿#define TGDEBUG
using UnityEngine;
using System.Collections;
using System.Diagnostics; // for ConditionalAttribute

namespace ThreeGlasses
{

    public static class ThreeGlassesUtils
    {
        // 复写log
        [ConditionalAttribute("TGDEBUG")]
        public static void Log(object msg)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(msg);
#endif
        }

        public static Component CopyComponent(Component original, GameObject destination)
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            // Copied fields can be restricted with BindingFlags
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy;
        }

        public static bool CheckNaN(Vector3 vec)
        {
            return !float.IsNaN(vec.x) && !float.IsNaN(vec.y) && !float.IsNaN(vec.z);
        }
    }
}