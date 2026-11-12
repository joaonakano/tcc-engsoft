using System;
using System.Collections.Generic;

public class P { public string Name; public double Pf, Gd, Ms; }

public class Params
{
    // novo modelo
    public double MaxHealth = 100, PerfectHeal = 3, GoodHeal = 1.8, MissDamage = 7;
    public double ComboForMax = 10, MinComboMult = 1.0, MaxComboMult = 1.5;
    public double MissEscalation = 0.25; public int EscalationCap = 4;
    public double LowThreshold = 0.35, MercyMin = 0.5, ComebackMax = 1.8;
    public double S1 = 0.30, S2 = 0.55, S3 = 0.80;
}

public static class Sim
{
    static Random rnd = new Random(12345);

    static int Outcome(P p, bool burst)
    {
        double pf = burst ? 0.05 : p.Pf, gd = burst ? 0.35 : p.Gd; // resto = miss
        if (burst) { pf = p.Pf * 0.5; gd = p.Gd * 0.7; }
        double r = rnd.NextDouble();
        if (r < pf) return 2;
        if (r < pf + gd) return 1;
        return 0;
    }

    // modo: "old" ou "new". retorna (morreu, percent, fraçãoDaMúsicaJogada)
    public static string Run(string mode, P p, int notes, int runs, Params q)
    {
        int deaths = 0; int[] stars = new int[4]; double sumPct = 0;
        for (int r = 0; r < runs; r++)
        {
            double h = 100; int combo = 0, missStreak = 0; double gained = 0; int score = 0; bool burst = false, dead = false;
            for (int i = 0; i < notes; i++)
            {
                burst = burst ? rnd.NextDouble() > 0.2 : rnd.NextDouble() < 0.02;
                int o = Outcome(p, burst);
                if (o > 0)
                {
                    score += o;
                    if (mode == "old")
                    {
                        double mult = 0.3 + (1.5 - 0.3) * Math.Min(1.0, combo / 20.0);
                        double heal = (o == 2 ? 5 : 3) * mult;
                        double before = h; h = Math.Min(100, h + heal); gained += h - before; combo++;
                    }
                    else
                    {
                        double cm = q.MinComboMult + (q.MaxComboMult - q.MinComboMult) * Math.Min(1.0, combo / q.ComboForMax);
                        double pct = h / q.MaxHealth;
                        double cb = pct < q.LowThreshold ? q.ComebackMax + (1 - q.ComebackMax) * (pct / q.LowThreshold) : 1;
                        h = Math.Min(q.MaxHealth, h + (o == 2 ? q.PerfectHeal : q.GoodHeal) * cm * cb);
                        combo++; missStreak = 0;
                    }
                }
                else
                {
                    if (mode == "old") { h = Math.Max(0, h - (10 + gained * 0.5)); gained = 0; combo = 0; }
                    else
                    {
                        double pct = h / q.MaxHealth;
                        double mercy = pct < q.LowThreshold ? q.MercyMin + (1 - q.MercyMin) * (pct / q.LowThreshold) : 1;
                        double dmg = q.MissDamage * (1 + q.MissEscalation * Math.Min(missStreak, q.EscalationCap)) * mercy;
                        h = Math.Max(0, h - dmg); missStreak++; combo = 0;
                    }
                    if (h <= 0) { dead = true; break; }
                }
            }
            if (dead) deaths++;
            double max = notes * 2.0; double percent = score / max; sumPct += percent;
            int st = percent >= q.S3 ? 3 : percent >= q.S2 ? 2 : percent >= q.S1 ? 1 : 0;
            if (mode == "old") st = percent >= 0.9 ? 3 : percent >= 0.7 ? 2 : percent >= 0.4 ? 1 : 0;
            stars[st]++;
        }
        return string.Format("{0,-9} morre {1,3:0}% | media {2,3:0}% | 0*:{3,3:0}% 1*:{4,3:0}% 2*:{5,3:0}% 3*:{6,3:0}%",
            p.Name, 100.0 * deaths / runs, 100 * sumPct / runs, 100.0 * stars[0] / runs, 100.0 * stars[1] / runs, 100.0 * stars[2] / runs, 100.0 * stars[3] / runs);
    }

    public static string All(string mode, Params q)
    {
        var ps = new List<P> {
            new P{Name="iniciante", Pf=.10, Gd=.60, Ms=.30}, new P{Name="casual", Pf=.25, Gd=.60, Ms=.15},
            new P{Name="bom", Pf=.45, Gd=.50, Ms=.05}, new P{Name="expert", Pf=.70, Gd=.29, Ms=.01},
            new P{Name="ruim", Pf=.05, Gd=.50, Ms=.45} };
        var sb = new System.Text.StringBuilder();
        foreach (var p in ps) sb.AppendLine(Run(mode, p, 450, 3000, q));
        return sb.ToString();
    }
}
