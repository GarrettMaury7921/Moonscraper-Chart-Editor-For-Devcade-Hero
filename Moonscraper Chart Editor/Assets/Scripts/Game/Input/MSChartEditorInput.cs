﻿// Copyright (c) 2016-2020 Alexander Ong
// Copyright (c) 2023 Garrett Maury
// See LICENSE in project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonscraperEngine;
using MoonscraperEngine.Input;

// HOW TO ADD A NEW ACTION
/*
 * 1. Add to the enum list
 * 2. Open Scenes/Config Editors/Input Editor
 * 3. Click on the Input Builder object and locate the Input Config Builder script
 * 4. Click the button "Load Config From File", which will load /Assets/Database/InputPropertiesConfig.json
 * 5. The field InputProperties/Shortcut Input will now be populated. Scroll down to find your new action and set up the new properties
 * 6. Click the button "Save Config To File" and overwrite InputPropertiesConfig.json
 */

public enum MSChartEditorInputActions
{
    ActionHistoryRedo,
    ActionHistoryUndo,

    AddSongObject,

    BpmIncrease,
    BpmDecrease,

    ChordSelect,

    ClipboardCopy,
    ClipboardCut,
    ClipboardPaste,

    Delete, 

    FileLoad,
    FileNew,
    FileSave,
    FileSaveAs,

    LyricEditorSetTime,

    MoveStepPositive,
    MoveStepNegative,
    MoveMeasurePositive,
    MoveMeasureNegative,

    NoteSetNatural,
    NoteSetStrum,
    NoteSetHopo,
    NoteSetTap,
    NoteSetTom,
    NoteSetCymbal,
    NoteSetDynamicsNone,
    NoteSetAccent,
    NoteSetGhost,
    
    PlayPause,

    ReloadAudio,

    SelectAll,
    SelectAllSection,
    StepDecrease,
    StepIncrease,
    StepDecreaseBy1,
    StepIncreaseBy1,

    SectionJumpPositive,
    SectionJumpNegative,
    SectionJumpMouseScroll,

    ToggleBpmAnchor,
    ToggleClap,
    ToggleExtendedSustains,
    ToggleMetronome,
    ToggleMouseMode,
    ToggleNoteForced,
    ToggleNoteTap,
    ToggleNoteCymbal,
    ToggleNoteDoubleKick,
    ToggleNoteAccent,
    ToggleNoteGhost,
    ToggleStarpowerDrumsFillActivation,
    ToggleViewMode, 

    DrumRollSetSingle,
    DrumRollSetDouble,

    ToolNoteBurst,
    ToolNoteHold,
    ToolSelectCursor,
    ToolSelectEraser,
    ToolSelectNote,
    ToolSelectStarpower,
    ToolSelectBpm,
    ToolSelectTimeSignature,
    ToolSelectSection,
    ToolSelectEvent,
    ToolSelectDrumRoll,

    ToolNoteLane1,
    ToolNoteLane2,
    ToolNoteLane3,
    ToolNoteLane4,
    ToolNoteLane5,
    ToolNoteLane6,
    ToolNoteLane7,
    ToolNoteLane8,
    ToolNoteLane9,
    ToolNoteLaneOpen,

    CloseMenu,

    // Guitar Actions
    GuitarStrumUp,
    GuitarStrumDown,

    GuitarFretGreen,
    GuitarFretRed,
    GuitarFretYellow,
    GuitarFretBlue,
    GuitarFretOrange,

    // Drum Actions
    DrumPadRed,
    DrumPadYellow,
    DrumPadBlue,
    DrumPadOrange,
    DrumPadGreen,
    DrumPadKick,

    // Pro Drums Actions
    DrumPadProRedTom,
    DrumPadProRedCymbal,
    DrumPadProYellowTom,
    DrumPadProYellowCymbal,
    DrumPadProBlueTom,
    DrumPadProBlueCymbal,
    DrumPadProOrangeTom,
    DrumPadProOrangeCymbal,
    DrumPadProGreenTom,
    DrumPadProGreenCymbal,
    DrumPadProKick1,

    Whammy,
    StartGameplay,
}

public static class MSChartEditorInput
{
    public static class Category
    {
        // static int to make int conversion way easier. The lack of implicit enum->int conversion is annoying as hell.
        public enum CategoryType
        {
            // Add new categories to the bottom of the list! Order is saved out in properties file.
            Global,
            Editor,
            EditorKeyboardMode,
            EditorToolNote,
            
            GameplayGuitar,
            GameplayDrums,
            GameplayDrumsPro,

            EditorToolGroupNote,
            EditorToolStarpower,
            EditorToolGroupNoteGuitar,
            EditorToolGroupNoteDrums,

            EditorToolDrumRoll,
        }

        public static InteractionMatrix interactionMatrix = new InteractionMatrix(EnumX<CategoryType>.Count);
        public static readonly int kEditorCategoryMask 
            = (1 << (int)CategoryType.Editor)
            | (1 << (int)CategoryType.EditorKeyboardMode)
            | (1 << (int)CategoryType.EditorToolNote)
            | (1 << (int)CategoryType.EditorToolGroupNote)
            | (1 << (int)CategoryType.EditorToolStarpower)
            | (1 << (int)CategoryType.Global)
            | (1 << (int)CategoryType.EditorToolGroupNoteGuitar)
            | (1 << (int)CategoryType.EditorToolGroupNoteDrums)
            | (1 << (int)CategoryType.EditorToolDrumRoll)
            ;
        public static readonly int kGameplayCategoryMask = (1 << (int)CategoryType.GameplayGuitar) | (1 << (int)CategoryType.GameplayDrums) | (1 << (int)CategoryType.GameplayDrumsPro);
        public static readonly int kGameplayGuitarCategoryMask = (1 << (int)CategoryType.GameplayGuitar);
        public static readonly int kGameplayDrumsCategoryMask = (1 << (int)CategoryType.GameplayDrums);
        public static readonly int kGameplayDrumsProCategoryMask = (1 << (int)CategoryType.GameplayDrumsPro);

        static Category()
        {
            interactionMatrix.SetInteractableAll((int)CategoryType.Global);

            interactionMatrix.SetInteractable((int)CategoryType.EditorToolGroupNoteGuitar, (int)CategoryType.EditorToolGroupNote);
            interactionMatrix.SetInteractable((int)CategoryType.EditorToolGroupNoteDrums, (int)CategoryType.EditorToolGroupNote);

            // Set editor interactions
            foreach (var value in EnumX<CategoryType>.Values)
            {
                // Every category interacts with itself
                interactionMatrix.SetInteractable((int)value, (int)value);

                // All editor inputs conflict with Top-Level editor categories
                if (((1 << (int)value) & kEditorCategoryMask) != 0)
                    interactionMatrix.SetInteractable((int)value, (int)CategoryType.Editor);
            }
        }
    }

    const bool kRebindableDefault = true;
    const bool kHiddenInListsDefault = false;
    const int kCategoryDefault = (int)Category.CategoryType.Editor;

    static readonly InputAction.Properties kDefaultProperties = new InputAction.Properties { rebindable = kRebindableDefault, hiddenInLists = kHiddenInListsDefault, category = kCategoryDefault };

    public class MSChartEditorActionContainer : InputActionContainer<MSChartEditorInputActions>
    {
        public MSChartEditorActionContainer()  : base(new EnumLookupTable<MSChartEditorInputActions, InputAction>())
        {
            InputManager inputManager = InputManager.Instance;

            for (int i = 0; i < actionConfigCleanLookup.Count; ++i)
            {
                MSChartEditorInputActions scEnum = (MSChartEditorInputActions)i;
                InputAction.Properties properties;
                if (!inputManager.inputProperties.TryGetPropertiesConfig(scEnum, out properties))
                {
                    properties = kDefaultProperties;
                }

                if (string.IsNullOrEmpty(properties.displayName))
                {
                    properties.displayName = scEnum.ToString();
                }

                actionConfigCleanLookup[scEnum] = new InputAction(properties);
            }
        }
    }

    static MSChartEditorActionContainer primaryInputs { get { return Globals.gameSettings.controls; } }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region Input Queries
    // Note, bool allowedDuringRebind should actually be replaced with flags of some kind. Just need quick fix right now.

    static bool BlockedByRebind(bool allowedDuringRebind)
    {
        return !allowedDuringRebind && Services.IsBindingsMenuActive;
    }

    public static bool GetInputDown(MSChartEditorInputActions key, bool allowedDuringRebind = false)
    {
        if (ChartEditor.hasFocus && !Services.IsTyping && !BlockedByRebind(allowedDuringRebind))
        {
            return primaryInputs.GetActionConfig(key).GetInputDown(InputManager.Instance.devices);
        }

        return false;
    }

    public static bool GetInputUp(MSChartEditorInputActions key, bool allowedDuringRebind = false)
    {
        if (ChartEditor.hasFocus && !Services.IsTyping && !BlockedByRebind(allowedDuringRebind))
        {
            return primaryInputs.GetActionConfig(key).GetInputUp(InputManager.Instance.devices);
        }

        return false;
    }

    public static bool GetInput(MSChartEditorInputActions key, bool allowedDuringRebind = false)
    {
        if (ChartEditor.hasFocus && !Services.IsTyping && !BlockedByRebind(allowedDuringRebind))
        {
            return primaryInputs.GetActionConfig(key).GetInput(InputManager.Instance.devices);
        }

        return false;
    }

    public static float GetAxis(MSChartEditorInputActions key, bool allowedDuringRebind = false)
    {
        float? value = GetAxisMaybe(key, allowedDuringRebind);
        return value.HasValue ? value.Value : 0;
    }

    public static float? GetAxisMaybe(MSChartEditorInputActions key, bool allowedDuringRebind = false)
    {
        if (ChartEditor.hasFocus && !Services.IsTyping && !BlockedByRebind(allowedDuringRebind))
        {
            return primaryInputs.GetActionConfig(key).GetAxisMaybe(InputManager.Instance.devices);
        }

        return null;
    }

    public static bool GetGroupInputDown(MSChartEditorInputActions[] keys, bool allowedDuringRebind = false)
    {
        foreach (MSChartEditorInputActions key in keys)
        {
            if (GetInputDown(key, allowedDuringRebind))
                return true;
        }

        return false;
    }

    public static bool GetGroupInputUp(MSChartEditorInputActions[] keys, bool allowedDuringRebind = false)
    {
        foreach (MSChartEditorInputActions key in keys)
        {
            if (GetInputUp(key, allowedDuringRebind))
                return true;
        }

        return false;
    }

    public static bool GetGroupInput(MSChartEditorInputActions[] keys, bool allowedDuringRebind = false)
    {
        foreach (MSChartEditorInputActions key in keys)
        {
            if (GetInput(key, allowedDuringRebind))
                return true;
        }

        return false;
    }

    #endregion
}
