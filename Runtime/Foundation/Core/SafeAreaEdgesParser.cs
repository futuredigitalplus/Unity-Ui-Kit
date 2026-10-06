using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Parses the <c>edges</c> UXML attribute: <c>all</c>, <c>none</c>, or a list such as <c>top,bottom</c> (commas, spaces or both, any case).</summary>
    public static class SafeAreaEdgesParser
    {
        /// <summary>Parses the attribute value. Empty or null gives <see cref="SafeAreaEdges.All"/>; unknown words are ignored.</summary>
        public static SafeAreaEdges Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return SafeAreaEdges.All;
            SafeAreaEdges result = SafeAreaEdges.None;
            string[] parts = value.Split(new[] { ',', ' ', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                switch (parts[i].ToLowerInvariant())
                {
                    case "all":
                        result |= SafeAreaEdges.All;
                        break;
                    case "left":
                        result |= SafeAreaEdges.Left;
                        break;
                    case "right":
                        result |= SafeAreaEdges.Right;
                        break;
                    case "top":
                        result |= SafeAreaEdges.Top;
                        break;
                    case "bottom":
                        result |= SafeAreaEdges.Bottom;
                        break;
                    case "horizontal":
                        result |= SafeAreaEdges.Left | SafeAreaEdges.Right;
                        break;
                    case "vertical":
                        result |= SafeAreaEdges.Top | SafeAreaEdges.Bottom;
                        break;
                }
            }
            return result;
        }
    }
}
