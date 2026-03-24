using System.Collections.Generic;
using Supervertaler.Trados.Models;

namespace Supervertaler.Trados.Core
{
    /// <summary>
    /// Estimates token counts and API costs for AI calls.
    /// Uses chars/4 heuristic for token estimation (no external library).
    /// Pricing table based on official provider rates as of March 2026.
    /// </summary>
    public static class TokenEstimator
    {
        // Per-million-token pricing: (input, output)
        private static readonly Dictionary<string, (decimal inputPer1M, decimal outputPer1M)> Pricing
            = new Dictionary<string, (decimal, decimal)>
        {
            // OpenAI
            { "gpt-4.1",                   (2.00m,   8.00m)  },
            { "gpt-4.1-mini",              (0.40m,   1.60m)  },
            { "gpt-5.4",                   (10.00m,  30.00m) },
            { "o4-mini",                   (1.10m,   4.40m)  },

            // Claude (Anthropic)
            { "claude-sonnet-4-6",         (3.00m,   15.00m) },
            { "claude-haiku-4-5-20251001", (0.80m,   4.00m)  },
            { "claude-opus-4-6",           (15.00m,  75.00m) },

            // Google Gemini
            { "gemini-2.5-flash",          (0.15m,   0.60m)  },
            { "gemini-2.5-pro",            (1.25m,   10.00m) },
            { "gemini-3.1-pro-preview",    (2.00m,   12.00m) },

            // Grok (xAI)
            { "grok-4.20-0309-non-reasoning", (3.00m,  15.00m) },
            { "grok-4-1-fast-non-reasoning",  (0.20m,  0.50m)  },
            { "grok-4.20-0309-reasoning",     (3.00m,  15.00m) },

            // Ollama (local) — free
            { "translategemma:12b",        (0m, 0m) },
            { "translategemma:4b",         (0m, 0m) },
            { "qwen3:14b",                (0m, 0m) },
            { "aya-expanse:8b",            (0m, 0m) },
        };

        /// <summary>
        /// Estimates token count from a string using chars/4 heuristic.
        /// Returns 0 for null/empty strings.
        /// </summary>
        public static int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return (text.Length + 3) / 4; // ceil division
        }

        /// <summary>
        /// Estimates total input tokens for a SendPromptAsync call.
        /// </summary>
        public static int EstimateInputTokens(string userPrompt, string systemPrompt)
        {
            return EstimateTokens(userPrompt) + EstimateTokens(systemPrompt);
        }

        /// <summary>
        /// Estimates total input tokens for a SendChatAsync call.
        /// </summary>
        public static int EstimateInputTokens(List<ChatMessage> messages, string systemPrompt)
        {
            int total = EstimateTokens(systemPrompt);
            if (messages != null)
            {
                foreach (var msg in messages)
                    total += EstimateTokens(msg.Content);
            }
            return total;
        }

        /// <summary>
        /// Estimates the cost of an API call in USD.
        /// Returns 0 for unknown models or Ollama (local).
        /// </summary>
        public static decimal EstimateCost(string model, int inputTokens, int outputTokens)
        {
            if (string.IsNullOrEmpty(model)) return 0m;

            (decimal inputPer1M, decimal outputPer1M) rates;
            if (!Pricing.TryGetValue(model, out rates))
                return 0m;

            return (inputTokens * rates.inputPer1M / 1_000_000m)
                 + (outputTokens * rates.outputPer1M / 1_000_000m);
        }

        /// <summary>
        /// Returns true if pricing information is available for the given model.
        /// </summary>
        public static bool HasPricing(string model)
        {
            return !string.IsNullOrEmpty(model) && Pricing.ContainsKey(model);
        }
    }
}
