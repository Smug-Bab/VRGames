using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Dynamic Voxel Block", menuName = "Modular Engine/Dynamic Block Definition")]
public class VoxelBlockDefinition : ScriptableObject
{
    [Header("Identity")]
    public bool isSolid = true;
    public bool isOpaque = true;

    [Header("Raw Formula Input")]
    [Tooltip("Example: 10(SiO2)+2 (Al2O3)-1 (Fe2O3) (H2O) (C100H100O50N5)")]
    public string fullChemicalFormula = "";

    [Header("Parsed Compound Matrix")]
    public List<VoxelElementCompound> compounds = new List<VoxelElementCompound>();

    [Header("Assumed Block Color")]
    [Tooltip("Base color derived purely from the chemical formula for inspector display.")]
    public Color assumedBlockColor = Color.white;

    [Header("Calculated Physics Properties")]
    public float starterHeat = 0f;
    public float thermalConductivity = 1.0f;
    public float thermalInsulation = 1.0f;
    public bool isRadioactive = false;

    public const float RADIATION_THRESHOLD = 1f;

    [Header("Color Blending Tunables")]
    // Tunables removed to keep color derivation strictly formula-driven.
    // Internal constants used instead of inspector-exposed knobs:
    private const float MASS_ALPHA = 0.9f;
    private const float WEIGHT_POWER = 0.85f;
    private const bool USE_SOFTMAX = false;
    private const float SOFT_K = 1.0f;
    // Extra multiplier to make computed pigments visually significant versus raw elemental colors
    private const float PIGMENT_SCALE = 3.0f;
    // Visual boost applied when blending computed pigments into the final color
    private const float PIGMENT_VISUAL_BOOST = 4.0f;

    [Tooltip("Enable to log the top element contributors for a sampled voxel when computing color.")]
    public bool debugContributors = false;

    // Simple contributor container (avoids C# tuple syntax for older Unity compilers)
    private class Contributor
    {
        public string symbol;
        public float rawWeight;
        public Contributor(string s, float w) { symbol = s; rawWeight = w; }
    }

    public bool IsFormulaValid(out string errorMessage)
    {
        errorMessage = "";
        if (string.IsNullOrWhiteSpace(fullChemicalFormula)) return true;

        int openParen = 0;
        foreach (char c in fullChemicalFormula)
        {
            if (c == '(') openParen++;
            if (c == ')') openParen--;
            if (openParen < 0)
            {
                errorMessage = "Formula contains an unmatched closing parenthesis ')'.";
                return false;
            }
        }

        if (openParen != 0)
        {
            errorMessage = "Formula contains an unmatched opening parenthesis '('.";
            return false;
        }

        return true;
    }

    public void ParseFullFormulaIntoArray()
    {
        if (string.IsNullOrWhiteSpace(fullChemicalFormula) || !IsFormulaValid(out _))
        {
            compounds.Clear();
            RecalculateBlockPhysics();
            assumedBlockColor = Color.white;
            return;
        }

        string pattern = @"(\d*\([^()]+\)(?:[\+\-]\d*)?)(?:([=#-])|(?=\s|\(|$|\d*\())";
        MatchCollection matches = Regex.Matches(fullChemicalFormula, pattern);

        List<VoxelElementCompound> newCompounds = new List<VoxelElementCompound>();

        for (int i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];
            string fullSmiles = match.Groups[1].Value;
            string bondSymbol = match.Groups[2].Value;

            SMILESBondType interBond = SMILESBondType.None;
            if (bondSymbol == "-") interBond = SMILESBondType.Single;
            else if (bondSymbol == "=") interBond = SMILESBondType.Double;
            else if (bondSymbol == "#") interBond = SMILESBondType.Triple;

            VoxelElementCompound compoundToUse = new VoxelElementCompound
            {
                smilesFormula = fullSmiles,
                baseConcentration = 0.5f,
                noiseVariance = 0.2f,
                bondToNextCompound = interBond
            };

            compoundToUse.ParseConstituentElements();
            newCompounds.Add(compoundToUse);
        }

        compounds = newCompounds;
        RecalculateBlockPhysics();

        // Update the inspector-displayed assumed block color purely from the formula (ignoring noise coordinates)
        assumedBlockColor = GetDynamicBlockColor(0, 0, 0, 0);
    }

    public void RecalculateBlockPhysics()
    {
        starterHeat = 0f;
        int netTotalCharge = 0;

        foreach (var compound in compounds)
        {
            if (compound == null) continue;

            starterHeat += compound.GetCompoundRadiation();
            netTotalCharge += compound.netCharge;
        }

        isRadioactive = starterHeat >= RADIATION_THRESHOLD;

        if (netTotalCharge > 0)
        {
            thermalConductivity = 1.0f + (netTotalCharge * 0.5f);
            thermalInsulation = 1.0f / thermalConductivity;
        }
        else if (netTotalCharge < 0)
        {
            thermalInsulation = 1.0f + (Mathf.Abs(netTotalCharge) * 0.5f);
            thermalConductivity = 1.0f / thermalInsulation;
        }
        else
        {
            thermalConductivity = 1.0f;
            thermalInsulation = 1.0f;
        }
    }

    public Color CalculatedBlockColor => GetDynamicBlockColor(0, 0, 0, 0);

    public Color GetDynamicBlockColor(int globalX, int globalY, int globalZ, int seed)
    {
        if (compounds == null || compounds.Count == 0) return Color.magenta;

        // Color blending uses internal constants (MASS_ALPHA, WEIGHT_POWER, USE_SOFTMAX, SOFT_K)

        // helper: visual influence factors to prevent light common atoms (O, H) from washing out color
        float VisualWeightFactor(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return 1f;
            switch (symbol)
            {
                case "H":
                case "O":
                    return 0.35f; // reduce visual influence of hydrogen/oxygen
                case "He": case "Ne": case "Ar": case "Kr": case "Xe": case "Rn":
                    return 0.25f; // noble gases negligible visual influence
                case "C":
                    return 1.0f; // carbon keeps normal influence
                case "Fe": case "Cu": case "Mn": case "Co": case "Ni": case "Cr": case "Ti": case "V":
                    return 1.5f; // transition metals get slightly amplified influence
                case "S": case "P":
                    return 0.9f;
                default:
                    return 1.0f;
            }
        }

        // We'll accumulate in linear color space to avoid sRGB averaging issues
        Vector3 accumLinear = Vector3.zero;
        float totalWeight = 0f;
        // Pigment accumulators (separate so pigments can be blended deterministically)
        Vector3 pigmentLinearAccum = Vector3.zero;
        float pigmentTotalWeight = 0f;

        // track contributors for optional debugging
        var contributors = new System.Collections.Generic.List<Contributor>();

        foreach (var compound in compounds)
        {
            if (compound == null || string.IsNullOrWhiteSpace(compound.smilesFormula)) continue;

            float concentration = compound.GetDynamicConcentration(globalX, globalY, globalZ, seed);
            var elements = compound.ParseAndExposeBonds();

            int localAtomTotal = 0;

            foreach (var kvp in elements)
            {
                if (kvp.Key == "__INVALID_SYNTAX__") continue;

                var elem = VoxelElementRegistry.Get(kvp.Key);
                if (elem != null)
                {
                    Color elemColor = elem.GetColor();
                    Color elemLinear = elemColor.linear;

                    float influence = VisualWeightFactor(kvp.Key);
                    float visualWeight = 1f;
                    try { visualWeight = elem.visualWeight; } catch { visualWeight = 1f; }

                    // raw weight before normalization
                    float raw = kvp.Value * concentration * Mathf.Pow(Mathf.Max(1f, elem.atomicMass), MASS_ALPHA) * influence * visualWeight;

                        contributors.Add(new Contributor(kvp.Key, raw));

                    localAtomTotal += kvp.Value;

                    // optional softmax or power scaling

                        float scaled = raw;
                        if (USE_SOFTMAX)
                            scaled = Mathf.Exp(SOFT_K * raw);
                        else if (!Mathf.Approximately(WEIGHT_POWER, 1f))
                            scaled = Mathf.Pow(Mathf.Max(0f, raw), WEIGHT_POWER);

                        accumLinear += new Vector3(elemLinear.r, elemLinear.g, elemLinear.b) * scaled;
                        totalWeight += scaled;
                    }
                }

                // Detect computed pigment signal from this compound (no fallback table)
                if (elements != null && elements.Count > 0)
                {
                    Color pigmentColor;
                    float pigmentStrength;
                    DetectPigment(elements, out pigmentColor, out pigmentStrength);
                    if (pigmentStrength > 0.02f)
                    {
                        // Scale pigment contribution by concentration and local atom count to keep deterministic
                        float pigmentContribution = pigmentStrength * concentration * (Mathf.Max(1, localAtomTotal) / 10f) * compound.quantityMultiplier * PIGMENT_SCALE;
                        Color pigLin = pigmentColor.linear;
                        Vector3 pigVec = new Vector3(pigLin.r, pigLin.g, pigLin.b) * pigmentContribution;
                        // Accumulate pigments separately so we can blend them in proportion to element weights
                        pigmentLinearAccum += pigVec;
                        pigmentTotalWeight += pigmentContribution;
                        contributors.Add(new Contributor("<pigment>", pigmentContribution));
                    }
                }
        }

        if (totalWeight <= 0f) return Color.white;

        Vector3 averagedLinear = accumLinear / totalWeight;

        // If pigments were detected, blend their average linear color into the averaged result
        if (pigmentTotalWeight > 0f)
        {
            Vector3 pigmentAvgLinear = pigmentLinearAccum / pigmentTotalWeight;
            float pigmentBlend = Mathf.Clamp01(pigmentTotalWeight / (totalWeight + pigmentTotalWeight)) * PIGMENT_VISUAL_BOOST;
            averagedLinear = Vector3.Lerp(averagedLinear, pigmentAvgLinear, pigmentBlend);
            // reflect pigment blending in totalWeight for any later heuristics
            totalWeight += pigmentTotalWeight * PIGMENT_VISUAL_BOOST;
        }
        Color averaged = new Color(averagedLinear.x, averagedLinear.y, averagedLinear.z, 1f).gamma;

        // Convert to HSV and slightly boost saturation to avoid washed-out greys
        Color.RGBToHSV(averaged, out float h, out float s, out float v);

        // Increase saturation proportionally to presence of heavy/metallic elements
        float heavyFactor = Mathf.Clamp01((totalWeight / (compounds.Count * 40f)));
        s = Mathf.Clamp01(s * (1.0f + 0.5f * heavyFactor) + 0.03f);

        // Reduce value slightly if the result is near neutral grey to increase contrast
        float greyness = 1f - (Mathf.Abs(averaged.r - averaged.g) + Mathf.Abs(averaged.g - averaged.b) + Mathf.Abs(averaged.r - averaged.b)) / 3f;
        if (greyness > 0.08f)
        {
            v = Mathf.Clamp01(v * (1.0f - (greyness * 0.25f)));
        }

        Color finalColor = Color.HSVToRGB(h, s, v);

        // Apply mild gamma correction to give slightly warmer tones
        finalColor.r = Mathf.Pow(finalColor.r, 0.92f);
        finalColor.g = Mathf.Pow(finalColor.g, 0.92f);
        finalColor.b = Mathf.Pow(finalColor.b, 0.92f);

        // Optional debug: print top contributors
        if (debugContributors)
        {
            contributors.Sort((a, b) => b.rawWeight.CompareTo(a.rawWeight));
            int limit = Mathf.Min(6, contributors.Count);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"VoxelBlockColor contributors for '{name}': totalWeight={totalWeight:F3}");
            for (int i = 0; i < limit; i++)
            {
                var c = contributors[i];
                sb.AppendLine($"  {i+1}. {c.symbol} -> raw={c.rawWeight:F3}");
            }
            Debug.Log(sb.ToString());
        }

        return finalColor;
    }

    // Heuristic pigment detection computed from element counts; returns a color and a 0..1 strength
    private void DetectPigment(System.Collections.Generic.Dictionary<string,int> counts, out Color pigmentColor, out float strength)
    {
        int C = counts.TryGetValue("C", out int c) ? c : 0;
        int H = counts.TryGetValue("H", out int h) ? h : 0;
        int N = counts.TryGetValue("N", out int n) ? n : 0;
        int O = counts.TryGetValue("O", out int o) ? o : 0;
        int Mg = counts.TryGetValue("Mg", out int mg) ? mg : 0;
        int Fe = counts.TryGetValue("Fe", out int fe) ? fe : 0;
        int S = counts.TryGetValue("S", out int s) ? s : 0;

        float total = Mathf.Max(1f, C + H + N + O + S);

        // Approximate double bond equivalents (DBE) as a proxy for conjugation
        float dbe = C - (H * 0.5f) + (N * 0.5f) + 1f;
        float heteroFrac = (N + O + S) / total;
        float metalFactor = (Mg > 0 || Fe > 0) ? 1f : 0f;

        float raw = dbe * 0.12f + metalFactor * 0.9f + heteroFrac * 0.4f;
        strength = Mathf.Clamp01(raw / 2f);

        // Decide pigment hue by deterministic heuristics
        if (Mg > 0 && N >= 4 && C >= 40 && C <= 70)
        {
            // Chlorophyll-like
            pigmentColor = new Color(0.12f, 0.6f, 0.12f);
            strength *= 1.2f;
            strength = Mathf.Clamp01(strength);
            return;
        }

        if (dbe >= 8f && O < (C * 0.3f))
        {
            // Carotenoid-like
            pigmentColor = new Color(0.9f, 0.5f, 0.06f);
            return;
        }

        if (Fe > 0)
        {
            pigmentColor = new Color(0.6f, 0.15f, 0.05f);
            return;
        }

        float cfrac = C / total;
        if (cfrac > 0.4f)
        {
            // Humic / organic brown
            pigmentColor = new Color(0.25f, 0.15f, 0.08f);
            strength *= Mathf.Clamp01(cfrac);
            return;
        }

        // Default: no strong pigment
        pigmentColor = Color.white;
        strength *= 0.05f;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(VoxelBlockDefinition))]
public class VoxelBlockDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        VoxelBlockDefinition blockDef = (VoxelBlockDefinition)target;

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck())
        {
            blockDef.ParseFullFormulaIntoArray();
            EditorUtility.SetDirty(blockDef);
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Parse & Validate Formula Matrix"))
        {
            blockDef.ParseFullFormulaIntoArray();
            EditorUtility.SetDirty(blockDef);
        }

        // Button to log top color contributors for a sample voxel
        if (GUILayout.Button("Log Top Color Contributors (sample 0,0,0)"))
        {
            bool prev = blockDef.debugContributors;
            blockDef.debugContributors = true;
            // sample at origin (0,0,0) with seed 0
            blockDef.GetDynamicBlockColor(0, 0, 0, 0);
            blockDef.debugContributors = prev;
        }

        EditorGUILayout.Space(10);

        if (!blockDef.IsFormulaValid(out string syntaxError))
        {
            EditorGUILayout.HelpBox($"Syntax Error: {syntaxError}", MessageType.Error);
        }

        if (blockDef.compounds != null && blockDef.compounds.Count > 0)
        {
            List<string> invalidElements = new List<string>();

            foreach (var compound in blockDef.compounds)
            {
                if (compound == null) continue;
                var extractedAtoms = compound.ParseConstituentElements();

                foreach (var kvp in extractedAtoms)
                {
                    if (kvp.Key == "__INVALID_SYNTAX__")
                    {
                        EditorGUILayout.HelpBox($"Invalid Syntax in compound '{compound.smilesFormula}'.", MessageType.Error);
                        continue;
                    }

                    if (!VoxelElementRegistry.ContainsElement(kvp.Key))
                    {
                        if (!invalidElements.Contains(kvp.Key))
                            invalidElements.Add(kvp.Key);
                    }
                }
            }

            if (invalidElements.Count > 0)
            {
                EditorGUILayout.HelpBox($"Invalid Element Error: Unrecognized symbol(s): '{string.Join(", ", invalidElements)}'", MessageType.Error);
            }

            if (blockDef.isRadioactive)
            {
                EditorGUILayout.HelpBox($"WARNING: High Radiation / Starter Heat ({blockDef.starterHeat} Heat Units). Block is classified as Radioactive!", MessageType.Warning);
            }
        }

        EditorGUILayout.Space(10);

        if (blockDef.compounds != null && blockDef.compounds.Count > 0)
        {
            EditorGUILayout.LabelField("Parsed Compound Topology & Physics", EditorStyles.boldLabel);

            for (int i = 0; i < blockDef.compounds.Count; i++)
            {
                var compound = blockDef.compounds[i];
                if (compound == null) continue;

                string chargeText = compound.netCharge != 0 ? $" [Net Charge: {compound.netCharge}]" : "";
                string multText = compound.quantityMultiplier > 1 ? $" [Multiplier: {compound.quantityMultiplier}x]" : "";

                EditorGUILayout.HelpBox($"Compound [{i}]: {compound.smilesFormula}{multText}{chargeText}", MessageType.Info);
            }
        }
    }
}
#endif
