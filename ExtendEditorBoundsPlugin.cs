using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using UnityEngine;
using HarmonyLib;

namespace ExtendEditorBounds
{
    [BepInPlugin("uwu.speen.extendeditorbounds", "Extend Editor Bounds", "0.1.0")]
    public class ExtendEditorBoundsPlugin : BaseUnityPlugin
    {
        public static BepInEx.Logging.ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            Harmony.CreateAndPatchAll(typeof(ExtendEditorBounds));
        }

        [HarmonyPatch(typeof(TrackEditorGUI), "MoveNotesInLateralDirection")]
        public class ExtendEditorBounds
        {
            static bool Prefix(int lateralDirection, TrackEditorGUI __instance, HashSet<int> ___s_tempHashSet)
            {
                PlayableTrackData trackData = __instance.frameInfo.trackData;
                if (trackData == null)
                {
                    return false;
                }
                int num = -lateralDirection;
                bool moveElements = false;

                var editorInputType = typeof(TrackEditorGUI).GetNestedType("EditorInput");
                var editorInputInstanceField = typeof(TrackEditorGUI).GetField("s_movementInput", BindingFlags.Static | BindingFlags.NonPublic);

                if (editorInputType != null && editorInputInstanceField != null)
                {
                    var editorInputInstance = (ValueType)editorInputInstanceField.GetValue(null);
                    var moveElementsField = editorInputType.GetField("moveElements");

                    if (moveElementsField != null)
                    {
                        moveElements = (bool)moveElementsField.GetValue(editorInputInstance);
                    }
                }

                var beginEditing = AccessTools.Method(typeof(TrackEditorGUI), "BeginEditing");
                beginEditing.Invoke(__instance, new object[] { });
                ___s_tempHashSet.Clear();
                
                foreach (int num2 in trackData.NoteIsSelected)
                {
                    Note note = trackData.GetNote(num2);
                    if (note.IsSectionContinuation)
                    {
                        SpinnerSection? sectionContainingNote = trackData.NoteData.SpinnerSections.GetSectionContainingNote(num2);
                        if (sectionContainingNote != null)
                        {
                            int u_num2 = sectionContainingNote.GetValueOrDefault().FirstNoteIndex;
                            note = trackData.GetNote(u_num2);
                        }
                    }
                    if (___s_tempHashSet.Add(num2))
                    {
                        if (moveElements)
                        {
                            int numEndTypesForNoteIndex = trackData.GetNumEndTypesForNoteIndex(num2);
                            if (numEndTypesForNoteIndex <= 0)
                            {
                                continue;
                            }
                            int num3 = ((note.unfilteredSize - 1).ClampIndex(numEndTypesForNoteIndex.GetIndexRange()) + lateralDirection).Repeat(numEndTypesForNoteIndex);
                            note.unfilteredSize = num3 + 1;
                        }
                        else if (note.IsSpinner)
                        {
                            note.SpinDirection = -note.SpinDirection;
                        }
                        else
                        {
                            if (note.IsWholeBarNote)
                            {
                                continue;
                            }
                            int num4 = 100;
                            int num5 = note.column + num;
                            note.column = (sbyte)Mathf.Clamp(num5, -num4, num4);
                        }
                        trackData.SetNote(note, num2);
                    }
                }

                var endEditing = AccessTools.Method(typeof(TrackEditorGUI), "EndEditing");
                endEditing.Invoke(__instance, new object[] { });
                
                return false;
            }
        }
    }
}
