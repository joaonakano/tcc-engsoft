using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Alinha os tempos das notas (vindos do MIDI, com BPM fixo/aproximado) aos ATAQUES reais do áudio.
///
/// Por que: no FL Studio o áudio pode acelerar/desacelerar (ou ter swing, padding, etc.) enquanto o MIDI
/// continua preso ao mapa de tempo. A diferença cresce ao longo da música e "tudo fica fora do ritmo".
///
/// Como:
///  1. Detecta onsets no áudio (fluxo espectral, STFT) com precisão de ~1 ms.
///  2. Cada nota procura o onset mais forte/próximo numa janela curta ao redor do seu tempo + deriva estimada.
///  3. A deriva (onset - nota) é estimada localmente pela mediana dos vizinhos casados, então notas sem onset
///     claro (ex.: sustentadas, silenciosas) acompanham o andamento real em vez do MIDI cru.
/// </summary>
public static class BeatmapAudioAligner
{
    private const int FftSize = 1024;
    private const int Hop = 256;

    private const double MaxSearch = 0.12;      // s: quanto uma nota pode andar para casar com um onset (além da deriva)
    private const double MinSearch = 0.03;
    private const double GapFraction = 0.45;    // janela <= 45% da distância até a nota vizinha
    private const int DriftNeighbors = 8;       // notas casadas de cada lado usadas na mediana da deriva

    public class Report
    {
        public bool ok;
        public string error;
        public int onsets, notes, matched;
        public double meanAbsShift, maxAbsShift, minDrift, maxDrift;
    }

    public static Report Align(AudioClip clip, double[] noteTimes)
    {
        var rep = new Report { notes = noteTimes.Length };

        if (clip == null) { rep.error = "sem AudioClip"; return rep; }
        if (noteTimes.Length == 0) { rep.error = "sem notas"; return rep; }

        float[] mono = ReadMono(clip, out int sampleRate);
        if (mono == null) { rep.error = "não consegui ler os samples do clip (Load Type = Decompress On Load?)"; return rep; }

        List<Onset> onsets = DetectOnsets(mono, sampleRate);
        rep.onsets = onsets.Count;
        if (onsets.Count < 4) { rep.error = "poucos onsets detectados no áudio"; return rep; }

        double[] original = (double[])noteTimes.Clone();
        int n = original.Length;

        // janela de busca por nota
        double[] window = new double[n];
        for (int i = 0; i < n; i++)
        {
            double gap = double.MaxValue;
            for (int j = i - 1; j >= 0; j--)
                if (original[i] - original[j] > 1e-4) { gap = Math.Min(gap, original[i] - original[j]); break; }
            for (int j = i + 1; j < n; j++)
                if (original[j] - original[i] > 1e-4) { gap = Math.Min(gap, original[j] - original[i]); break; }

            window[i] = Math.Max(MinSearch, Math.Min(MaxSearch, gap * GapFraction));
        }

        double[] drift = new double[n];     // deriva estimada por nota
        double[] residual = new double[n];
        bool[] hit = new bool[n];
        double[] snapped = new double[n];

        // 2 passadas: a primeira estima a deriva de trás pra frente (causal); a segunda refina usando vizinhos dos dois lados
        for (int pass = 0; pass < 2; pass++)
        {
            double running = 0;
            var recent = new List<double>();

            for (int i = 0; i < n; i++)
            {
                double expected = original[i] + (pass == 0 ? running : drift[i]);
                int best = BestOnset(onsets, expected, window[i]);

                hit[i] = best >= 0;
                if (hit[i])
                {
                    snapped[i] = onsets[best].time;
                    residual[i] = snapped[i] - original[i];

                    recent.Add(residual[i]);
                    if (recent.Count > DriftNeighbors) recent.RemoveAt(0);
                    running = Median(recent);
                }
            }

            // deriva centrada: mediana dos resíduos casados num raio de DriftNeighbors notas casadas
            ComputeCenteredDrift(hit, residual, drift);
        }

        // notas sem onset acompanham a deriva local
        double sumAbs = 0, maxAbs = 0, dMin = double.MaxValue, dMax = double.MinValue;
        int matched = 0;
        for (int i = 0; i < n; i++)
        {
            if (!hit[i]) snapped[i] = original[i] + drift[i];
            else matched++;

            double shift = snapped[i] - original[i];
            sumAbs += Math.Abs(shift);
            maxAbs = Math.Max(maxAbs, Math.Abs(shift));
            dMin = Math.Min(dMin, drift[i]);
            dMax = Math.Max(dMax, drift[i]);
        }

        // mantém ordem não decrescente (o spawner depende disso)
        for (int i = 1; i < n; i++)
            if (snapped[i] < snapped[i - 1]) snapped[i] = snapped[i - 1];

        Array.Copy(snapped, noteTimes, n);

        rep.ok = true;
        rep.matched = matched;
        rep.meanAbsShift = sumAbs / n;
        rep.maxAbsShift = maxAbs;
        rep.minDrift = dMin;
        rep.maxDrift = dMax;
        return rep;
    }

    // ---------------------------------------------------------
    // Casamento / deriva
    // ---------------------------------------------------------

    private static int BestOnset(List<Onset> onsets, double center, double window)
    {
        int lo = LowerBound(onsets, center - window);
        int best = -1;
        double bestScore = 0;

        for (int k = lo; k < onsets.Count && onsets[k].time <= center + window; k++)
        {
            double closeness = 1.0 - Math.Abs(onsets[k].time - center) / window;
            double score = onsets[k].strength * (0.25 + 0.75 * closeness);
            if (score > bestScore) { bestScore = score; best = k; }
        }
        return best;
    }

    private static int LowerBound(List<Onset> onsets, double t)
    {
        int lo = 0, hi = onsets.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (onsets[mid].time < t) lo = mid + 1; else hi = mid;
        }
        return lo;
    }

    private static void ComputeCenteredDrift(bool[] hit, double[] residual, double[] drift)
    {
        int n = hit.Length;
        var matchedIdx = new List<int>();
        for (int i = 0; i < n; i++) if (hit[i]) matchedIdx.Add(i);

        if (matchedIdx.Count == 0) { Array.Clear(drift, 0, n); return; }

        var tmp = new List<double>();
        int cursor = 0; // primeiro matchedIdx >= i
        for (int i = 0; i < n; i++)
        {
            while (cursor < matchedIdx.Count && matchedIdx[cursor] < i) cursor++;

            int from = Math.Max(0, cursor - DriftNeighbors);
            int to = Math.Min(matchedIdx.Count - 1, cursor + DriftNeighbors - 1);

            tmp.Clear();
            for (int k = from; k <= to; k++) tmp.Add(residual[matchedIdx[k]]);
            drift[i] = Median(tmp);
        }
    }

    private static double Median(List<double> values)
    {
        if (values.Count == 0) return 0;
        var copy = new List<double>(values);
        copy.Sort();
        int m = copy.Count / 2;
        return copy.Count % 2 == 1 ? copy[m] : 0.5 * (copy[m - 1] + copy[m]);
    }

    // ---------------------------------------------------------
    // Detecção de onsets (fluxo espectral)
    // ---------------------------------------------------------

    private struct Onset
    {
        public double time;
        public double strength; // normalizado 0..1
    }

    private static float[] ReadMono(AudioClip clip, out int sampleRate)
    {
        sampleRate = clip.frequency;
        int channels = clip.channels;
        var data = new float[clip.samples * channels];

        try { if (!clip.GetData(data, 0)) return null; }
        catch (Exception) { return null; }

        if (channels == 1) return data;

        var mono = new float[clip.samples];
        for (int i = 0; i < mono.Length; i++)
        {
            float s = 0;
            for (int c = 0; c < channels; c++) s += data[i * channels + c];
            mono[i] = s / channels;
        }
        return mono;
    }

    private static List<Onset> DetectOnsets(float[] mono, int sampleRate)
    {
        int frames = (mono.Length - FftSize) / Hop;
        var onsets = new List<Onset>();
        if (frames < 8) return onsets;

        var window = new float[FftSize];
        for (int i = 0; i < FftSize; i++)
            window[i] = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (FftSize - 1)));

        int bins = FftSize / 2;
        var re = new float[FftSize];
        var im = new float[FftSize];
        var prev = new float[bins];
        var flux = new float[frames];

        for (int f = 0; f < frames; f++)
        {
            int start = f * Hop;
            for (int i = 0; i < FftSize; i++) { re[i] = mono[start + i] * window[i]; im[i] = 0f; }
            Fft(re, im);

            float sum = 0f;
            for (int b = 1; b < bins; b++)
            {
                float mag = Mathf.Log(1f + 100f * Mathf.Sqrt(re[b] * re[b] + im[b] * im[b]) / FftSize * 4f);
                float d = mag - prev[b];
                if (d > 0f) sum += d;
                prev[b] = mag;
            }
            flux[f] = f == 0 ? 0f : sum;
        }

        // limiar adaptativo: média local (~0.4 s) + fração do máximo global
        int radius = Mathf.Max(1, (int)(0.2 * sampleRate / Hop));
        float globalMax = 0f;
        foreach (float v in flux) if (v > globalMax) globalMax = v;
        if (globalMax <= 0f) return onsets;

        var csum = new double[frames + 1];
        for (int i = 0; i < frames; i++) csum[i + 1] = csum[i] + flux[i];

        const int peakRadius = 2;
        for (int f = peakRadius; f < frames - peakRadius; f++)
        {
            float v = flux[f];
            bool isPeak = true;
            for (int k = -peakRadius; k <= peakRadius && isPeak; k++)
                if (k != 0 && flux[f + k] > v) isPeak = false;
            if (!isPeak) continue;

            int a = Mathf.Max(0, f - radius), b = Mathf.Min(frames, f + radius + 1);
            double localMean = (csum[b] - csum[a]) / (b - a);

            if (v < localMean * 1.3 + 0.04f * globalMax) continue;

            // interpolação parabólica => precisão abaixo do hop
            float y0 = flux[f - 1], y1 = v, y2 = flux[f + 1];
            float denom = y0 - 2f * y1 + y2;
            float frac = Mathf.Abs(denom) > 1e-6f ? 0.5f * (y0 - y2) / denom : 0f;
            frac = Mathf.Clamp(frac, -0.5f, 0.5f);

            // O fluxo cresce mais rápido quando o ataque está a ~1/4 da janela de Hann (e não no centro):
            // por isso o instante do ataque é "início do frame + FftSize/4".
            double samplePos = (f + frac) * Hop + FftSize / 4.0;

            onsets.Add(new Onset { time = samplePos / sampleRate, strength = Mathf.Clamp01(v / globalMax) });
        }

        return onsets;
    }

    // FFT radix-2 iterativa, in-place
    private static void Fft(float[] re, float[] im)
    {
        int n = re.Length;

        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j)
            {
                (re[i], re[j]) = (re[j], re[i]);
                (im[i], im[j]) = (im[j], im[i]);
            }
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            float ang = -2f * Mathf.PI / len;
            float wr = Mathf.Cos(ang), wi = Mathf.Sin(ang);
            for (int i = 0; i < n; i += len)
            {
                float cr = 1f, ci = 0f;
                for (int k = 0; k < len / 2; k++)
                {
                    int u = i + k, v = i + k + len / 2;
                    float tr = re[v] * cr - im[v] * ci;
                    float ti = re[v] * ci + im[v] * cr;
                    re[v] = re[u] - tr; im[v] = im[u] - ti;
                    re[u] += tr; im[u] += ti;

                    float ncr = cr * wr - ci * wi;
                    ci = cr * wi + ci * wr;
                    cr = ncr;
                }
            }
        }
    }
}
