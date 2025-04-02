using System;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class SpritePreviewInDictionaryAttribute : Attribute { }

#if UNITY_EDITOR

[DrawerPriority(0, 0, 0)] //Makes sure this is run first
public class
    SpritePreviewInDictionaryDrawer : OdinAttributeDrawer<SpritePreviewInDictionaryAttribute,
        Dictionary<string, Sprite>>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        //Load the dictionary / instantiate it if it doesn't exist
        var dictionary = this.ValueEntry.SmartValue;
        if (dictionary == null)
        {
            this.ValueEntry.SmartValue = new Dictionary<string, Sprite>();
            dictionary = this.ValueEntry.SmartValue;
        }

        //Iterate over every item in the dictionary and draw it
        foreach (var key in new List<string>(dictionary.Keys))
        {
            EditorGUILayout.BeginHorizontal();

            //Draw the sprite
            dictionary[key] = (Sprite)EditorGUILayout.ObjectField(dictionary[key], typeof(Sprite), false,
                GUILayout.Width(70), GUILayout.Height(70));

            //Draw the key as a label
            EditorGUILayout.LabelField(key, GUILayout.Width(150));

            EditorGUILayout.EndHorizontal();
        }
    }
}

#endif