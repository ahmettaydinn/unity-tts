using System;
using System.Collections.Generic;
using LocalTTS.G2P;
using LocalTTS.Kokoro;
using Unity.InferenceEngine;
using UnityEngine;

namespace LocalTTS
{
    /// <summary>
    /// The main entry point: plain English text in, speech audio out. Owns the model
    /// worker and the G2P pipeline. Create once, reuse for all characters, dispose on
    /// shutdown. All public methods must be called from the main thread.
    /// </summary>
    public sealed class TTSEngine : IDisposable
    {
        /// <summary>Silence inserted between sentences, in samples (~120 ms).</summary>
        private const int SentenceGapSamples = TTSSettings.OutputSampleRate * 12 / 100;

        private readonly Worker worker;
        private readonly EnglishG2P g2p;
        private readonly TTSSettings settings;
        private readonly Queue<AwaitableCompletionSource> waiters =
            new Queue<AwaitableCompletionSource>();
        private bool busy;

        public TTSSettings Settings => settings;
        public EnglishG2P G2P => g2p;

        private TTSEngine(Worker worker, EnglishG2P g2p, TTSSettings settings)
        {
            this.worker = worker;
            this.g2p = g2p;
            this.settings = settings;
        }

        /// <summary>
        /// Loads the model, parses the lexicon off the main thread, and warms the
        /// backend up so the first real line has no compilation hitch.
        /// </summary>
        public static async Awaitable<TTSEngine> CreateAsync(
            ModelAsset modelAsset, TTSSettings settings = null, KokoroVoice warmupVoice = null)
        {
            settings ??= TTSSettings.Default;

            byte[] lexiconBytes = Lexicon.ReadPackedBytes(); // main thread (Resources)

            await Awaitable.BackgroundThreadAsync();
            var lexicon = Lexicon.FromGzipBytes(lexiconBytes);
            await Awaitable.MainThreadAsync();

            var model = ModelLoader.Load(modelAsset);
            var worker = new Worker(model, settings.Backend == TTSBackend.GpuCompute
                ? BackendType.GPUCompute
                : BackendType.CPU);

            var engine = new TTSEngine(worker, new EnglishG2P(lexicon), settings);

            // GPU shader compilation costs ~19s on a cold machine; pay it here, not on
            // the first dialogue line.
            if (warmupVoice != null)
            {
                await engine.SynthesizePhonemesAsync("ˈO kˈA.", warmupVoice, 1f);
            }

            return engine;
        }

        /// <summary>
        /// Synthesizes English text. Concurrent calls are serviced strictly in FIFO
        /// order — safe to call from many characters at once.
        /// </summary>
        public async Awaitable<SynthesisResult> SynthesizeAsync(
            string text, KokoroVoice voice, float speed = 0f)
        {
            if (voice == null)
            {
                throw new ArgumentNullException(nameof(voice));
            }

            if (busy)
            {
                // Wait for our turn; the finishing request hands ownership over
                // directly (busy never drops to false in between), so no caller
                // can jump the queue.
                var turn = new AwaitableCompletionSource();
                waiters.Enqueue(turn);
                await turn.Awaitable;
            }
            else
            {
                busy = true;
            }

            try
            {
                if (speed <= 0f)
                {
                    speed = settings.DefaultSpeed;
                }

                // Text processing is pure C# — keep it off the main thread.
                await Awaitable.BackgroundThreadAsync();
                List<string> sentences = TextNormalizer.NormalizeAndSplit(text);
                var phonemeSentences = new List<string>(sentences.Count);
                foreach (string sentence in sentences)
                {
                    string phonemes = g2p.PhonemizeToString(sentence);
                    if (phonemes.Length > 0)
                    {
                        phonemeSentences.Add(phonemes);
                    }
                }

                await Awaitable.MainThreadAsync();

                var chunks = new List<float[]>(phonemeSentences.Count);
                long totalSamples = 0;
                foreach (string phonemes in phonemeSentences)
                {
                    float[] samples = await RunModelAsync(phonemes, voice, speed);
                    chunks.Add(samples);
                    totalSamples += samples.Length;
                }

                var combined = new float[totalSamples + Math.Max(0, chunks.Count - 1) * SentenceGapSamples];
                int offset = 0;
                for (int i = 0; i < chunks.Count; i++)
                {
                    chunks[i].CopyTo(combined, offset);
                    offset += chunks[i].Length + SentenceGapSamples;
                }

                return new SynthesisResult(combined, phonemeSentences.ToArray());
            }
            finally
            {
                if (waiters.Count > 0)
                {
                    waiters.Dequeue().SetResult(); // ownership passes to the next request
                }
                else
                {
                    busy = false;
                }
            }
        }

        /// <summary>Synthesizes a raw phoneme string (advanced / testing use).</summary>
        public async Awaitable<SynthesisResult> SynthesizePhonemesAsync(
            string phonemes, KokoroVoice voice, float speed = 1f)
        {
            float[] samples = await RunModelAsync(phonemes, voice, speed);
            return new SynthesisResult(samples, new[] { phonemes });
        }

        /// <summary>Worst single-frame scheduling stall of the most recent request, in ms.</summary>
        public double LastMaxFrameStallMs { get; private set; }

        /// <summary>Frames the most recent request spread its scheduling across.</summary>
        public int LastScheduleFrames { get; private set; }

        private async Awaitable<float[]> RunModelAsync(string phonemes, KokoroVoice voice, float speed)
        {
            int[] ids = KokoroTokenizer.Encode(phonemes, out List<char> unknown);
            if (unknown.Count > 0)
            {
                Debug.LogWarning($"TTSEngine: symbols not in Kokoro vocab skipped: '{string.Concat(unknown)}'");
            }

            using var inputIds = new Tensor<int>(new TensorShape(1, ids.Length), ids);
            using var style = new Tensor<float>(
                new TensorShape(1, KokoroVoice.StyleDim), voice.GetStyle(ids.Length - 2));
            using var speedTensor = new Tensor<float>(new TensorShape(1), new[] { speed });

            LastMaxFrameStallMs = 0;
            LastScheduleFrames = 1;

            if (settings.FrameBudgetMs > 0f)
            {
                // Dispatch the graph layer by layer, yielding to the next frame whenever
                // this frame's scheduling budget is spent — no gameplay hitches.
                var iterator = worker.ScheduleIterable(inputIds, style, speedTensor);
                var frameTimer = System.Diagnostics.Stopwatch.StartNew();
                while (iterator.MoveNext())
                {
                    if (frameTimer.Elapsed.TotalMilliseconds > settings.FrameBudgetMs)
                    {
                        LastMaxFrameStallMs = Math.Max(
                            LastMaxFrameStallMs, frameTimer.Elapsed.TotalMilliseconds);
                        await Awaitable.NextFrameAsync();
                        LastScheduleFrames++;
                        frameTimer.Restart();
                    }
                }

                LastMaxFrameStallMs = Math.Max(
                    LastMaxFrameStallMs, frameTimer.Elapsed.TotalMilliseconds);
            }
            else
            {
                var stallTimer = System.Diagnostics.Stopwatch.StartNew();
                worker.Schedule(inputIds, style, speedTensor);
                LastMaxFrameStallMs = stallTimer.Elapsed.TotalMilliseconds;
            }

            using var output = await ((Tensor<float>)worker.PeekOutput()).ReadbackAndCloneAsync();
            return output.DownloadToArray();
        }

        public void Dispose() => worker?.Dispose();
    }
}
