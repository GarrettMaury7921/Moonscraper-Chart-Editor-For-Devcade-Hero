﻿// Copyright (c) 2016-2020 Alexander Ong
// Copyright (c) 2023 Garrett Maury
// See LICENSE in project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SustainResources : ScriptableObject
{
    [Header("Sustain Colours (Line Renderer)")]
    public Material[] sustainColours = new Material[8];
    public Material[] ghlSustainColours = new Material[9];
}
