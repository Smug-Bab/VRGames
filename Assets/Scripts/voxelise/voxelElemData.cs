using System.Collections.Generic;

public sealed class Element
{
    public readonly string Symbol;
    public readonly double Mass;           // atomic mass (g/mol)
    public readonly float Bond;            // 0..1
    public readonly float Conductivity;    // 0..1
    public readonly float Radioactivity;   // 0..1

    public Element(string symbol, double mass, float bond, float conductivity, float radioactivity = 0f)
    { Symbol = symbol; Mass = mass; Bond = bond; Conductivity = conductivity; Radioactivity = radioactivity; }
}

public static class PeriodicTable
{
    static readonly Dictionary<string, Element> table = new Dictionary<string, Element>();
    static void Add(Element e) => table[e.Symbol] = e;

    static PeriodicTable()
    {
        Add(new Element("H", 1.0080, 0.7f, 0.0f, 0.0f));
        Add(new Element("He", 4.0026, 0.0f, 0.0f, 0.0f));
        Add(new Element("Li", 6.94, 0.3f, 0.8f, 0.0f));
        Add(new Element("Be", 9.0122, 0.4f, 0.4f, 0.0f));
        Add(new Element("B", 10.81, 0.6f, 0.1f, 0.0f));
        Add(new Element("C", 12.011, 0.9f, 0.2f, 0.0f));
        Add(new Element("N", 14.007, 0.7f, 0.0f, 0.0f));
        Add(new Element("O", 15.999, 0.8f, 0.0f, 0.0f));
        Add(new Element("F", 18.998, 0.9f, 0.0f, 0.0f));
        Add(new Element("Ne", 20.180, 0.0f, 0.0f, 0.0f));
        Add(new Element("Na", 22.990, 0.3f, 0.9f, 0.0f));
        Add(new Element("Mg", 24.305, 0.4f, 0.6f, 0.0f));
        Add(new Element("Al", 26.982, 0.5f, 0.9f, 0.0f));
        Add(new Element("Si", 28.085, 0.7f, 0.3f, 0.0f));
        Add(new Element("P", 30.974, 0.6f, 0.0f, 0.0f));
        Add(new Element("S", 32.06, 0.6f, 0.0f, 0.0f));
        Add(new Element("Cl", 35.45, 0.8f, 0.0f, 0.0f));
        Add(new Element("Ar", 39.95, 0.0f, 0.0f, 0.0f));
        Add(new Element("K", 39.098, 0.3f, 0.9f, 0.0f));
        Add(new Element("Ca", 40.078, 0.4f, 0.7f, 0.0f));
        Add(new Element("Sc", 44.956, 0.5f, 0.5f, 0.0f));
        Add(new Element("Ti", 47.867, 0.6f, 0.5f, 0.0f));
        Add(new Element("V", 50.942, 0.6f, 0.5f, 0.0f));
        Add(new Element("Cr", 51.996, 0.6f, 0.6f, 0.0f));
        Add(new Element("Mn", 54.938, 0.6f, 0.4f, 0.0f));
        Add(new Element("Fe", 55.845, 0.7f, 0.5f, 0.0f));
        Add(new Element("Co", 58.933, 0.6f, 0.5f, 0.0f));
        Add(new Element("Ni", 58.693, 0.6f, 0.6f, 0.0f));
        Add(new Element("Cu", 63.546, 0.5f, 1.0f, 0.0f));
        Add(new Element("Zn", 65.38, 0.4f, 0.4f, 0.0f));
        Add(new Element("Ga", 69.723, 0.4f, 0.3f, 0.0f));
        Add(new Element("Ge", 72.630, 0.6f, 0.2f, 0.0f));
        Add(new Element("As", 74.922, 0.6f, 0.1f, 0.0f));
        Add(new Element("Se", 78.971, 0.6f, 0.0f, 0.0f));
        Add(new Element("Br", 79.904, 0.8f, 0.0f, 0.0f));
        Add(new Element("Kr", 83.798, 0.0f, 0.0f, 0.0f));
        Add(new Element("Rb", 85.468, 0.3f, 0.9f, 0.0f));
        Add(new Element("Sr", 87.62, 0.4f, 0.6f, 0.0f));
        Add(new Element("Y", 88.906, 0.5f, 0.4f, 0.0f));
        Add(new Element("Zr", 91.224, 0.6f, 0.3f, 0.0f));
        Add(new Element("Nb", 92.906, 0.6f, 0.3f, 0.0f));
        Add(new Element("Mo", 95.95, 0.6f, 0.4f, 0.0f));
        Add(new Element("Tc", 97.0, 0.5f, 0.3f, 0.6f));
        Add(new Element("Ru", 101.07, 0.6f, 0.4f, 0.0f));
        Add(new Element("Rh", 102.91, 0.6f, 0.5f, 0.0f));
        Add(new Element("Pd", 106.42, 0.5f, 0.4f, 0.0f));
        Add(new Element("Ag", 107.87, 0.4f, 1.0f, 0.0f));
        Add(new Element("Cd", 112.41, 0.4f, 0.3f, 0.0f));
        Add(new Element("In", 114.82, 0.4f, 0.3f, 0.0f));
        Add(new Element("Sn", 118.71, 0.5f, 0.2f, 0.0f));
        Add(new Element("Sb", 121.76, 0.5f, 0.1f, 0.0f));
        Add(new Element("Te", 127.60, 0.5f, 0.1f, 0.0f));
        Add(new Element("I", 126.90, 0.7f, 0.0f, 0.0f));
        Add(new Element("Xe", 131.29, 0.0f, 0.0f, 0.0f));
        Add(new Element("Cs", 132.91, 0.3f, 0.9f, 0.0f));
        Add(new Element("Ba", 137.33, 0.4f, 0.5f, 0.0f));
        Add(new Element("La", 138.91, 0.4f, 0.3f, 0.0f));
        Add(new Element("Ce", 140.12, 0.4f, 0.2f, 0.0f));
        Add(new Element("Pr", 140.91, 0.4f, 0.2f, 0.0f));
        Add(new Element("Nd", 144.24, 0.4f, 0.2f, 0.0f));
        Add(new Element("Pm", 145.0, 0.4f, 0.2f, 0.8f));
        Add(new Element("Sm", 150.36, 0.4f, 0.2f, 0.1f));
        Add(new Element("Eu", 151.96, 0.4f, 0.2f, 0.1f));
        Add(new Element("Gd", 157.25, 0.4f, 0.2f, 0.1f));
        Add(new Element("Tb", 158.93, 0.4f, 0.2f, 0.1f));
        Add(new Element("Dy", 162.50, 0.4f, 0.2f, 0.1f));
        Add(new Element("Ho", 164.93, 0.4f, 0.2f, 0.1f));
        Add(new Element("Er", 167.26, 0.4f, 0.2f, 0.1f));
        Add(new Element("Tm", 168.93, 0.4f, 0.2f, 0.1f));
        Add(new Element("Yb", 173.05, 0.4f, 0.2f, 0.1f));
        Add(new Element("Lu", 174.97, 0.5f, 0.3f, 0.1f));
        Add(new Element("Hf", 178.49, 0.6f, 0.3f, 0.1f));
        Add(new Element("Ta", 180.95, 0.6f, 0.3f, 0.1f));
        Add(new Element("W", 183.84, 0.6f, 0.4f, 0.1f));
        Add(new Element("Re", 186.21, 0.6f, 0.3f, 0.2f));
        Add(new Element("Os", 190.23, 0.6f, 0.3f, 0.2f));
        Add(new Element("Ir", 192.22, 0.6f, 0.3f, 0.2f));
        Add(new Element("Pt", 195.08, 0.5f, 0.4f, 0.1f));
        Add(new Element("Au", 196.97, 0.4f, 0.9f, 0.1f));
        Add(new Element("Hg", 200.59, 0.3f, 0.1f, 0.1f));
        Add(new Element("Tl", 204.38, 0.4f, 0.2f, 0.2f));
        Add(new Element("Pb", 207.2, 0.4f, 0.2f, 0.3f));
        Add(new Element("Bi", 208.98, 0.4f, 0.1f, 0.4f));
        Add(new Element("Po", 209.0, 0.4f, 0.1f, 0.9f));
        Add(new Element("At", 210.0, 0.5f, 0.0f, 1.0f));
        Add(new Element("Rn", 222.0, 0.0f, 0.0f, 1.0f));
        Add(new Element("Fr", 223.0, 0.3f, 0.8f, 1.0f));
        Add(new Element("Ra", 226.0, 0.4f, 0.5f, 0.9f));
        Add(new Element("Ac", 227.0, 0.4f, 0.3f, 0.9f));
        Add(new Element("Th", 232.04, 0.5f, 0.3f, 0.8f));
        Add(new Element("Pa", 231.04, 0.5f, 0.3f, 0.9f));
        Add(new Element("U", 238.03, 0.6f, 0.3f, 0.9f));
        Add(new Element("Np", 237.0, 0.5f, 0.3f, 1.0f));
        Add(new Element("Pu", 244.0, 0.5f, 0.3f, 1.0f));
        Add(new Element("Am", 243.0, 0.5f, 0.2f, 1.0f));
        Add(new Element("Cm", 247.0, 0.5f, 0.2f, 1.0f));
        Add(new Element("Bk", 247.0, 0.5f, 0.2f, 1.0f));
        Add(new Element("Cf", 251.0, 0.5f, 0.2f, 1.0f));
        Add(new Element("Es", 252.0, 0.5f, 0.2f, 1.0f));
        Add(new Element("Fm", 257.0, 0.5f, 0.2f, 1.0f));
        Add(new Element("Md", 258.0, 0.5f, 0.2f, 1.0f));
        Add(new Element("No", 259.0, 0.5f, 0.2f, 1.0f));
        Add(new Element("Lr", 266.0, 0.5f, 0.2f, 1.0f));
        Add(new Element("Rf", 267.0, 0.5f, 0.3f, 1.0f));
        Add(new Element("Db", 268.0, 0.5f, 0.3f, 1.0f));
        Add(new Element("Sg", 267.0, 0.5f, 0.3f, 1.0f));
        Add(new Element("Bh", 270.0, 0.5f, 0.3f, 1.0f));
        Add(new Element("Hs", 271.0, 0.5f, 0.3f, 1.0f));
        Add(new Element("Mt", 278.0, 0.5f, 0.3f, 1.0f));
        Add(new Element("Ds", 281.0, 0.5f, 0.3f, 1.0f));
        Add(new Element("Rg", 282.0, 0.5f, 0.3f, 1.0f));
        Add(new Element("Cn", 285.0, 0.4f, 0.2f, 1.0f));
        Add(new Element("Nh", 286.0, 0.4f, 0.2f, 1.0f));
        Add(new Element("Fl", 289.0, 0.4f, 0.2f, 1.0f));
        Add(new Element("Mc", 290.0, 0.4f, 0.2f, 1.0f));
        Add(new Element("Lv", 293.0, 0.4f, 0.2f, 1.0f));
        Add(new Element("Ts", 294.0, 0.5f, 0.1f, 1.0f));
        Add(new Element("Og", 294.0, 0.0f, 0.0f, 1.0f));
    }

    public static Element Get(string sym)
    {
        if (!table.TryGetValue(sym, out var e))
            throw new System.ArgumentException($"Unknown element '{sym}'");
        return e;
    }
}
