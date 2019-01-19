﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoteFunctions {

    public static void GroupAddFlags(IList<Note> notes, Note.Flags flag, int index, int length)
    {
        for (int i = index; i < index + length; ++i)
        {
            notes[i].flags = notes[i].flags | flag;
        }
    }

    /// <summary>
    /// Gets all the notes (including this one) that share the same tick position as this one.
    /// </summary>
    /// <returns>Returns an array of all the notes currently sharing the same tick position as this note.</returns>
    public static Note[] GetChord(this Note note)
    {
        List<Note> chord = new List<Note>();
        chord.Add(note);
    
        Note previous = note.previous;
        while (previous != null && previous.tick == note.tick)
        {
            chord.Add(previous);
            previous = previous.previous;
        }
    
        Note next = note.next;
        while (next != null && next.tick == note.tick)
        {
            chord.Add(next);
            next = next.next;
        }
    
        return chord.ToArray();
    }

    public static void ApplyFlagsToChord(this Note note)
    {
        foreach (Note chordNote in note.chord)
        {
            chordNote.flags = note.flags;
        }
    }

    public static void GetPreviousOfSustains(List<Note> list, Note startNote, bool extendedSustainsEnabled)
    {
        list.Clear();

        Note previous = startNote.previous;

        int allVisited = startNote.gameMode == Chart.GameMode.GHLGuitar ? 63 : 31; // 0011 1111 for ghlive, 0001 1111 for standard
        int noteTypeVisited = 0;

        while (previous != null && noteTypeVisited < allVisited)
        {
            if (previous.IsOpenNote())
            {
                if (extendedSustainsEnabled)
                {
                    list.Add(previous);
                    return;
                }

                else if (list.Count > 0)
                    return;
                else
                {
                    list.Clear();
                    list.Add(previous);
                }
            }
            else if (previous.tick < startNote.tick)
            {
                if ((noteTypeVisited & (1 << previous.rawNote)) == 0)
                {
                    list.Add(previous);
                    noteTypeVisited |= 1 << previous.rawNote;
                }
            }

            previous = previous.previous;
        }
    }

    public static Note[] GetPreviousOfSustains(Note startNote, bool extendedSustainsEnabled)
    {
        List<Note> list = new List<Note>(6);

        GetPreviousOfSustains(list, startNote, extendedSustainsEnabled);

        return list.ToArray();
    }

    public static void CapSustain(this Note note, Note cap, Song song)
    {
        if (cap == null)
        {
            Debug.LogError("Cap sustain was not provided a note to cap with");
            return;
        }

        Note originalNote = (Note)note.Clone();
        note.length = GetCappedLength(note, cap, song);
    }

    public static uint GetCappedLength(this Note note, Note cap, Song song)
    {
        uint noteLength = note.length;

        // Cap sustain length
        if (cap.tick <= note.tick)
            noteLength = 0;
        else if (note.tick + note.length > cap.tick)        // Sustain extends beyond cap note 
        {
            noteLength = cap.tick - note.tick;
        }

        uint gapDis = (uint)(song.resolution * 4.0f / GameSettings.sustainGap);

        if (GameSettings.sustainGapEnabled && note.length > 0 && (note.tick + note.length > cap.tick - gapDis))
        {
            if ((int)(cap.tick - gapDis - note.tick) > 0)
                noteLength = cap.tick - gapDis - note.tick;
        }

        return noteLength;
    }

    public static Note FindNextSameFretWithinSustainExtendedCheck(this Note note, bool extendedSustainsEnabled)
    {
        Note next = note.next;

        while (next != null)
        {
            if (!extendedSustainsEnabled)
            {
                if ((next.IsOpenNote() || (note.tick < next.tick)) && note.tick != next.tick)
                    return next;
            }
            else
            {
                if ((!note.IsOpenNote() && next.IsOpenNote() && !(note.gameMode == Chart.GameMode.Drums)) || (next.rawNote == note.rawNote))
                    return next;
            }

            next = next.next;
        }

        return null;
    }

    public static bool IsOpenNote(this Note note)
    {
        if (note.gameMode == Chart.GameMode.GHLGuitar)
            return note.ghliveGuitarFret == Note.GHLiveGuitarFret.Open;
        else
            return note.guitarFret == Note.GuitarFret.Open;
    }

    /// <summary>
    /// Calculates and sets the sustain length based the tick position it should end at. Will be a length of 0 if the note position is greater than the specified position.
    /// </summary>
    /// <param name="pos">The end-point for the sustain.</param>
    public static void SetSustainByPos(this Note note, uint pos, Song song, bool extendedSustainsEnabled)
    {
        if (pos > note.tick)
            note.length = pos - note.tick;
        else
            note.length = 0;

        CapSustain(note, song, extendedSustainsEnabled);
    }

    public static void CapSustain(this Note note, Song song, bool extendedSustainsEnabled)
    {
        Note nextFret;
        nextFret = note.FindNextSameFretWithinSustainExtendedCheck(extendedSustainsEnabled);

        if (nextFret != null)
        {
            note.CapSustain(nextFret, song);
        }
    }

    public static void SetType(this Note note, Note.NoteType type)
    {
        note.flags = Note.Flags.None;
        switch (type)
        {
            case (Note.NoteType.Strum):
                if (note.isChord)
                    note.flags &= ~Note.Flags.Forced;
                else
                {
                    if (note.isNaturalHopo)
                        note.flags |= Note.Flags.Forced;
                    else
                        note.flags &= ~Note.Flags.Forced;
                }

                break;

            case (Note.NoteType.Hopo):
                if (!note.cannotBeForced)
                {
                    if (note.isChord)
                        note.flags |= Note.Flags.Forced;
                    else
                    {
                        if (!note.isNaturalHopo)
                            note.flags |= Note.Flags.Forced;
                        else
                            note.flags &= ~Note.Flags.Forced;
                    }
                }
                break;

            case (Note.NoteType.Tap):
                if (!note.IsOpenNote())
                    note.flags |= Note.Flags.Tap;
                break;

            default:
                break;
        }

        note.ApplyFlagsToChord();
    }

    public static int ExpensiveGetExtendedSustainMask(this Note note)
    {
        int mask = 0;

        if (note.length > 0 && note.chart != null)
        {
            int index, length;
            var notes = note.chart.notes;
            SongObjectHelper.GetRange(notes, note.tick, note.tick + note.length - 1, out index, out length);

            for (int i = index; i < index + length; ++i)
            {
                mask |= notes[i].mask;
            }
        }

        return mask;
    }

    public static Note.GuitarFret SaveGuitarNoteToDrumNote(Note.GuitarFret fret_type)
    {
        if (fret_type == Note.GuitarFret.Open)
            return Note.GuitarFret.Green;
        else if (fret_type == Note.GuitarFret.Orange)
            return Note.GuitarFret.Open;
        else
            return fret_type + 1;
    }

    public static Note.GuitarFret LoadDrumNoteToGuitarNote(Note.GuitarFret fret_type)
    {
        if (fret_type == Note.GuitarFret.Open)
            return Note.GuitarFret.Orange;
        else if (fret_type == Note.GuitarFret.Green)
            return Note.GuitarFret.Open;
        else
            return fret_type - 1;
    }

    public static void PerformPreChartInsertCorrections(Note note, Chart chart, IList<SongObject> newNotesAdded, IList<SongObject> oldNotesRemoved, bool extendedSustainsEnabled)
    {
        int index, length;
        SongObjectHelper.GetRange(chart.chartObjects, note.tick, note.tick, out index, out length);

        // Account for when adding an exact note as what's already in   
        if (length > 0)
        {
            for (int i = index + length - 1; i >= index; --i)
            {
                Note overwriteNote = chart.chartObjects[i] as Note;
                bool sameFret = note.guitarFret == overwriteNote.guitarFret;
                bool isOverwritableOpenNote = (note.IsOpenNote() || overwriteNote.IsOpenNote()) && !Globals.drumMode;
                if (overwriteNote != null && (isOverwritableOpenNote || sameFret))
                {
                    overwriteNote.Delete(false);
                    oldNotesRemoved.Add(overwriteNote);
                }
            }
        }
    }

    public static void PerformPostChartInsertCorrections(Note note, IList<SongObject> newNotesAdded, IList<SongObject> oldNotesRemoved, bool extendedSustainsEnabled)
    {
        Debug.Assert(note.chart != null, "Note has not been inserted into a chart");
        Debug.Assert(note.song != null, "Note has not been inserted into a song");

        Chart chart = note.chart;
        Song song = note.song;

        if (note.IsOpenNote())
            note.flags &= ~Note.Flags.Tap;

        if (note.cannotBeForced)
            note.flags &= ~Note.Flags.Forced;

        // Apply flags to chord
        foreach (Note chordNote in note.chord)
        {
            // Overwrite note flags
            if (chordNote.flags != note.flags)
            {
                Note newChordNote = new Note(chordNote.tick, chordNote.rawNote, chordNote.length, note.flags);
                AddOrReplaceNote(chart, chordNote, newChordNote, oldNotesRemoved, newNotesAdded);
            }
        }

        CapNoteCheck(chart, note, oldNotesRemoved, newNotesAdded, song, extendedSustainsEnabled);
        ForwardCap(chart, note, oldNotesRemoved, newNotesAdded, song);

        AutoForcedCheck(chart, note, oldNotesRemoved, newNotesAdded);
    }

    #region Note Insertion Helper Functions

    static Note FindReplacementNote(Note originalNote, IList<SongObject> replacementNotes)
    {
        foreach (Note replacementNote in replacementNotes)
        {
            if (replacementNote != null && originalNote.tick == replacementNote.tick && originalNote.rawNote == replacementNote.rawNote)
                return replacementNote;
        }

        return null;
    }

    static void AddOrReplaceNote(Chart chart, Note note, Note newNote, IList<SongObject> overwrittenList, IList<SongObject> replacementNotes)
    {
        Note replacementNote = FindReplacementNote(note, replacementNotes);
        if (replacementNote == null)
        {
            overwrittenList.Add(note);
            replacementNotes.Add(newNote);
            chart.Add(newNote);
        }
        else
        {
            replacementNote.CopyFrom(newNote);
        }
    }

    static void ForwardCap(Chart chart, Note note, IList<SongObject> overwrittenList, IList<SongObject> replacementNotes, Song song)
    {
        Note next;
        next = note.nextSeperateNote;

        if (!GameSettings.extendedSustainsEnabled)
        {
            // Get chord  
            next = note.nextSeperateNote;

            if (next != null)
            {
                foreach (Note noteToCap in note.chord)
                {
                    uint newLength = noteToCap.GetCappedLength(next, song);
                    if (noteToCap.length != newLength)
                    {
                        if (note == noteToCap)
                        {
                            note.length = newLength;
                        }
                        else
                        {
                            Note newNote = new Note(noteToCap.tick, noteToCap.rawNote, newLength, noteToCap.flags);
                            AddOrReplaceNote(chart, noteToCap, newNote, overwrittenList, replacementNotes);
                        }
                    }
                }
            }
        }
        else
        {
            // Find the next note of the same fret type or open
            next = note.next;
            while (next != null && next.guitarFret != note.guitarFret && !next.IsOpenNote())
                next = next.next;

            // If it's an open note it won't be capped

            if (next != null)
            {
                uint newLength = note.GetCappedLength(next, song);
                if (note.length != newLength)
                {
                    Note newNote = new Note(note.tick, note.rawNote, newLength, note.flags);
                    AddOrReplaceNote(chart, note, newNote, overwrittenList, replacementNotes);
                }
            }
        }
    }

    static void CapNoteCheck(Chart chart, Note noteToAdd, IList<SongObject> overwrittenList, IList<SongObject> replacementNotes, Song song, bool extendedSustainsEnabled)
    {
        Note[] previousNotes = NoteFunctions.GetPreviousOfSustains(noteToAdd, extendedSustainsEnabled);
        if (!GameSettings.extendedSustainsEnabled)
        {
            // Cap all the notes
            foreach (Note prevNote in previousNotes)
            {
                uint newLength = prevNote.GetCappedLength(noteToAdd, song);
                if (prevNote.length != newLength)
                {
                    Note newNote = new Note(prevNote.tick, prevNote.rawNote, newLength, prevNote.flags);
                    AddOrReplaceNote(chart, prevNote, newNote, overwrittenList, replacementNotes);
                }
            }

            foreach (Note chordNote in noteToAdd.chord)
            {
                uint newLength = noteToAdd.length;
                if (chordNote.length != newLength)
                {
                    if (noteToAdd == chordNote)
                    {
                        noteToAdd.length = newLength;
                    }
                    else
                    {
                        Note newNote = new Note(chordNote.tick, chordNote.rawNote, newLength, chordNote.flags);
                        AddOrReplaceNote(chart, chordNote, newNote, overwrittenList, replacementNotes);
                    }
                }
            }
        }
        else
        {
            // Cap only the sustain of the same fret type and open notes
            foreach (Note prevNote in previousNotes)
            {
                if (noteToAdd.IsOpenNote() || prevNote.guitarFret == noteToAdd.guitarFret)
                {
                    uint newLength = prevNote.GetCappedLength(noteToAdd, song);
                    if (prevNote.length != newLength)
                    {
                        overwrittenList.Add(prevNote);
                        chart.Add(new Note(prevNote.tick, prevNote.rawNote, newLength, prevNote.flags));
                    }
                }
            }
        }
    }

    static void AutoForcedCheck(Chart chart, Note note, IList<SongObject> overwrittenNotes, IList<SongObject> replacementNotes)
    {
        Note next = note.nextSeperateNote;
        if (next != null && (next.flags & Note.Flags.Forced) == Note.Flags.Forced && next.cannotBeForced)
        {
            Note.Flags flags = next.flags;
            flags &= ~Note.Flags.Forced;

            // Apply flags to chord
            foreach (Note chordNote in next.chord)
            {
                // Overwrite note flags
                if (chordNote.flags != flags)
                {
                    Note newChordNote = new Note(chordNote.tick, chordNote.rawNote, chordNote.length, note.flags);
                    AddOrReplaceNote(chart, chordNote, newChordNote, overwrittenNotes, replacementNotes);
                }
            }
        }
    }

    #endregion
}
