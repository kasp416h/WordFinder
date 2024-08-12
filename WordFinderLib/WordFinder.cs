﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace WordFinderLib
{
    public class WordFinder
    {
        private static int WordLength;
        private static int NumberOfWords;
        private static int TargetLength;
        private static ConcurrentBag<string> foundStuff = new ConcurrentBag<string>();
        private static string[] words;
        private static int[] masks;
        private static Dictionary<char, int> charFrequency;

        public event Action<int, int> ProgressChanged;
        public event Action<int> TotalFoundChanged;

        public WordFinder(int wordLength, int numberOfWords)
        {
            WordLength = wordLength;
            NumberOfWords = numberOfWords;
            TargetLength = WordLength * NumberOfWords;
        }

        public void Execute(string wordsFile)
        {
            var watch = new Stopwatch();
            words = LoadWords(wordsFile);
            Console.WriteLine("Words loaded: {0}", words.Length);
            watch.Start();

            CalculateCharFrequency();

            words = words.Where(w => w.Length == WordLength && w.Distinct().Count() == WordLength)
                         .OrderBy(word => word.Sum(c => charFrequency[c])).ToArray();

            masks = new int[words.Length];
            Parallel.For(0, words.Length, i =>
            {
                masks[i] = GetBitMask(words[i]);
            });

            var tasks = new List<Task>();
            int totalTasks = masks.Count(mask => mask != 0);
            int completedTasks = 0;

            for (int i = 0; i < masks.Length; i++)
            {
                if (masks[i] != 0)
                {
                    int index = i;
                     tasks.Add(Task.Run(() =>
                        {
                            FindFiveLetterWords(new int[NumberOfWords], 0, masks[index], index);
                            Interlocked.Increment(ref completedTasks);
                            ProgressChanged?.Invoke(completedTasks, totalTasks);
                        }));
                }
            }

            Task.WaitAll(tasks.ToArray());

            Console.WriteLine(foundStuff.Count());
            TotalFoundChanged?.Invoke(foundStuff.Count);
            watch.Stop();
            Console.WriteLine("Time {0} Ticks; {1} ms", watch.ElapsedTicks, watch.ElapsedMilliseconds);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FindFiveLetterWords(int[] combinationIndices, int depth, int combinationMask, int startingIndex)
        {
            combinationIndices[depth] = startingIndex;

            if (depth == NumberOfWords - 1)
            {
                var builder = new System.Text.StringBuilder();
                for (int i = 0; i < NumberOfWords; i++)
                {
                    if (i > 0) builder.Append(' ');
                    builder.Append(words[combinationIndices[i]]);
                }
                Console.WriteLine(builder.ToString());
                foundStuff.Add(builder.ToString());
                return;
            }

            for (int i = startingIndex - 1; i >= 0; i--)
            {
                if (masks[i] == 0 || (combinationMask & masks[i]) != 0) continue;

                int newMask = combinationMask | masks[i];
                if (IsPromising(depth + 1, newMask, i))
                {
                    FindFiveLetterWords(combinationIndices, depth + 1, newMask, i);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPromising(int currentCount, int newMask, int maxIndex)
        {
            int remainingWordsNeeded = NumberOfWords - currentCount;
            int uniqueLettersNeeded = TargetLength - CountBits(newMask);

            if (uniqueLettersNeeded > remainingWordsNeeded * WordLength)
            {
                return false;
            }

            int remainingWordsCount = maxIndex + 1;
            return remainingWordsCount >= remainingWordsNeeded;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountBits(int n)
        {
            return BitOperations.PopCount((uint)n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBitMask(string word)
        {
            int mask = 0;
            foreach (char c in word)
            {
                int bit = 1 << (c - 'a');
                if ((mask & bit) != 0) return 0;
                mask |= bit;
            }
            return mask;
        }

        private static void CalculateCharFrequency()
        {
            charFrequency = new Dictionary<char, int>();
            foreach (var word in words)
            {
                foreach (var c in word)
                {
                    if (charFrequency.ContainsKey(c))
                    {
                        charFrequency[c]++;
                    }
                    else
                    {
                        charFrequency[c] = 1;
                    }
                }
            }
        }

        private static string[] LoadWords(string wordsFile)
        {
            string directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filePath = Path.Combine(directory, wordsFile);
            return File.ReadAllLines(filePath);
        }
    }
}