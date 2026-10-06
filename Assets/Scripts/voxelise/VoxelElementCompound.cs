using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public enum SMILESBondType
{
    None,
    Single,
    Double,
    Triple
}

[Serializable]
public class VoxelElementCompound
{
    public string smilesFormula; // e.g., "10(SiO2)+2" or "(C100H100O50N5)"
    public float baseConcentration = 0.5f;
    public float noiseVariance = 0.2f;
    public SMILESBondType bondToNextCompound = SMILESBondType.None;

    public int quantityMultiplier = 1;
    public int netCharge = 0;

    /// <summary>
    /// Parses exterior quantity prefix, trailing charge, and interior element symbols.
    /// Example: "10(SiO2)+2" -> Multiplier: 10, Charge: +2, Elements: Si=10, O=20
    /// </summary>
    public Dictionary<string, int> ParseConstituentElements()
    {
        Dictionary<string, int> elementCounts = new Dictionary<string, int>();

        if (string.IsNullOrWhiteSpace(smilesFormula)) return elementCounts;

        string workString = smilesFormula.Trim();

        // 1. Extract Trailing Charge: e.g., "+", "-", "+2", "-3"
        Match chargeMatch = Regex.Match(workString, @"([\+\-])(\d*)$");
        if (chargeMatch.Success)
        {
            string sign = chargeMatch.Groups[1].Value;
            string valStr = chargeMatch.Groups[2].Value;
            int val = string.IsNullOrEmpty(valStr) ? 1 : int.Parse(valStr);
            netCharge = (sign == "+") ? val : -val;

            workString = workString.Substring(0, chargeMatch.Index).Trim();
        }
        else
        {
            netCharge = 0;
        }

        // 2. Extract Exterior Quantity Prefix and Inner Formula: e.g., "10(SiO2)" -> Prefix: 10, Inner: "SiO2"
        Match compoundMatch = Regex.Match(workString, @"^(\d*)\(([^()]+)\)$");
        string innerFormula = workString;

        if (compoundMatch.Success)
        {
            string multStr = compoundMatch.Groups[1].Value;
            quantityMultiplier = string.IsNullOrEmpty(multStr) ? 1 : int.Parse(multStr);
            innerFormula = compoundMatch.Groups[2].Value;
        }
        else
        {
            quantityMultiplier = 1;
            innerFormula = innerFormula.Replace("(", "").Replace(")", "").Trim();
        }

        // 3. Match Standard Elements: Capital letter + optional lowercase + count
        string pattern = @"([A-Z][a-z]*)(\d*)";
        MatchCollection matches = Regex.Matches(innerFormula, pattern);

        int matchedLength = 0;

        foreach (Match match in matches)
        {
            string symbol = match.Groups[1].Value;
            string countStr = match.Groups[2].Value;

            matchedLength += match.Length;

            int count = string.IsNullOrEmpty(countStr) ? 1 : int.Parse(countStr);
            int totalAtoms = count * quantityMultiplier;

            if (elementCounts.ContainsKey(symbol))
                elementCounts[symbol] += totalAtoms;
            else
                elementCounts[symbol] = totalAtoms;
        }

        if (matchedLength != innerFormula.Length)
        {
            elementCounts["__INVALID_SYNTAX__"] = 1;
        }

        return elementCounts;
    }

    /// <summary>
    /// Calculates Starter Heat based ONLY on exterior quantity multipliers (e.g. 10(SiO2)).
    /// Standard 1x compounds produce 0 radiation heat.
    /// </summary>
    public float GetCompoundRadiation()
    {
        ParseConstituentElements();

        // If multiplier is 1 or less, it contributes 0 starter heat/radiation
        if (quantityMultiplier <= 1) return 0f;

        // Scale heat by multiplier quantity above 1
        return (quantityMultiplier - 1) * 50f;
    }

    public Dictionary<string, int> ParseAndExposeBonds()
    {
        return ParseConstituentElements();
    }

    public float GetDynamicConcentration(int x, int y, int z, int seed)
    {
        return baseConcentration;
    }
}
