// By Olli S.

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;


public class RenameSelected : EditorWindow
{

    static string       baseName = "GameObject";
    static bool         applyIndex = true;
    static int          startIndex = 0;
    static int          increment = 1;
    static bool         addZeroes = true;
    static bool         dynamicDigits = true;
    static bool         performSort = true;
    static int          digits = 2;

    enum SortOrder
    {
        Ascending,
        Descending
    }

    static SortOrder sortOrder = SortOrder.Ascending;


    [MenuItem("GameObject/Olmi/Rename Selected", false, 0)]
    static void Perform(MenuCommand menuCommand)
    {
        if (Selection.objects.Length > 1)
        {
            if (menuCommand.context != Selection.objects[0])
            {
                return;
            }
        }

        RenameSelected window = ScriptableObject.CreateInstance<RenameSelected>();
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 200, 280);
        window.maxSize = new Vector2(200, 280);
        window.minSize = window.maxSize;
        window.ShowUtility();
    }


    static void Rename()
    {

        Dictionary <Transform, List<Transform>> transforms = new Dictionary<Transform, List<Transform>>();
        GameObject tempRoot = new GameObject();
        tempRoot.name = "tempRoot";


        foreach(Transform trans in Selection.transforms)
        {
            if (trans.parent != null)
            {
                if (transforms.ContainsKey(trans.parent))
                {
                    transforms[trans.parent].Add(trans);
                }
                else
                {
                    transforms[trans.parent] = new List<Transform> { trans };
                }
            }
            else
            {
                if (trans.root == trans)
                {
                    if (transforms.ContainsKey(tempRoot.transform))
                    {
                        transforms[tempRoot.transform].Add(trans);
                    }
                    else
                    {
                        transforms[tempRoot.transform] = new List<Transform> { trans };
                    }
                }
            }
        }


        foreach(KeyValuePair<Transform, List<Transform>> trans in transforms)
        {

            if (dynamicDigits)
            {
                digits = Mathf.Max(digits, trans.Value.Count.ToString().Length);
            }

            int postFix = startIndex;
            int lowestSiblingIndex = int.MaxValue;


            for(int c = 0; c < trans.Value.Count ; c++)
            {

                Undo.RecordObject(trans.Value[c].gameObject, "sibling rename for " + trans.Value[c].name);
                if (applyIndex)
                {
                    if (!addZeroes)
                    {
                        trans.Value[c].name = baseName + postFix;
                    }
                    else
                    {
                        digits = Mathf.Max(1, digits);
                        trans.Value[c].name = baseName + postFix.ToString("D" + digits);
                    }
                    postFix += increment;
                }
                else
                {
                    trans.Value[c].name = baseName;
                }

                int currentSiblingIndex = trans.Value[c].GetSiblingIndex();
                if (currentSiblingIndex < lowestSiblingIndex)
                {
                    lowestSiblingIndex = currentSiblingIndex;
                }
            }

            if (!performSort)
                continue;


            if (sortOrder == SortOrder.Ascending)
            {
                trans.Value.Sort((a, b) => a.name.CompareTo(b.name));
            }
            else if (sortOrder == SortOrder.Descending)
            {
                trans.Value.Sort((b, a) => a.name.CompareTo(b.name));
            }


            for(int i = 0; i < trans.Value.Count ; i++)
            {
                if (trans.Key != tempRoot.transform)
                {
                    Undo.SetTransformParent(trans.Value[i], trans.Key, "sibling index change for " + trans.Value[i].name);
                    trans.Value[i].SetSiblingIndex(i + lowestSiblingIndex);
                }
                else
                {
                    Undo.SetTransformParent(trans.Value[i], null, "sibling index change for " + trans.Value[i].name);
                    trans.Value[i].SetSiblingIndex(i + lowestSiblingIndex);
                }
            }
        }

        GameObject.DestroyImmediate(tempRoot);

    }


    void OnGUI()
    {
        EditorGUILayout.LabelField("Rename the selected objects in Hierarchy.", EditorStyles.wordWrappedLabel);

        GUILayout.Space(30);

        baseName = GUILayout.TextField(baseName, 25);

        GUILayout.Space(15);

        applyIndex = EditorGUILayout.Toggle(new GUIContent("Apply indexing: ", "If enabled, GameObjects will be numbered."), applyIndex);

        if (applyIndex)
        {
            startIndex = EditorGUILayout.IntField(new GUIContent("Start Index: ", "Index to start from."), startIndex);
            increment = EditorGUILayout.IntField(new GUIContent("Increment: ", "Number increment, stride in your numbers."), increment);
            addZeroes = EditorGUILayout.Toggle(new GUIContent("Add Zeroes: ", "Adds zeroes to the indexing: 01, 02..."), addZeroes);
            if (addZeroes)
            {
                dynamicDigits = EditorGUILayout.Toggle(new GUIContent("Dynamic Digits: ", "If the padding runs out (For example: 2 digits, but over 100 items), adjust padding dynamically."), dynamicDigits);
                digits = EditorGUILayout.IntField(new GUIContent("Digits: ", "How many digits to have. If you don't have dynamic padding active, you can generate inconsistent results, i.e. 01...101."), digits);
            }
            else
            {
                dynamicDigits = false;
                digits = 0;
            }
        }

        performSort = EditorGUILayout.Toggle(new GUIContent("Perform Sort: ", "Enable to sort the renamed objects. Note: This can change their position in the list as they get grouped together."), performSort);
        if (performSort)
        {
            sortOrder = (SortOrder) EditorGUILayout.EnumPopup(new GUIContent("Sort order: ", "Sorting direction, alphanumerical or reversed."), sortOrder);
        }

        // Validation
        startIndex = Mathf.Clamp(startIndex, 0, int.MaxValue-1);
        increment =  Mathf.Clamp(increment, 1, 100000);
        digits = Mathf.Clamp(digits, 1, 10);

        if (String.IsNullOrEmpty(baseName))
        {
            baseName = "GameObject";
        }

        GUILayout.Space(15);

        if (GUILayout.Button("Rename"))
        {
            Rename();
            this.Close();
        }
    }

}