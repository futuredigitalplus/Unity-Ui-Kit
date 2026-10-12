using System;

namespace FutureDigitalPlus.UIKit.Foundation.Responsive
{
    /// <summary>A screen of the game that the Responsive Manager can open (<see cref="IResponsivePreviewHost"/>): an id the game understands, a label and optional arguments.</summary>
    public sealed class ResponsivePreviewScreen
    {
        /// <summary>Creates an entry.</summary>
        /// <param name="id">What the game opens, for example a route name.</param>
        /// <param name="label">What the Manager shows; the id when empty.</param>
        /// <param name="args">Optional arguments for the game (a sample state), or null.</param>
        public ResponsivePreviewScreen(string id, string label = null, string args = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("A preview screen needs an id.", nameof(id));
            Id = id;
            Label = string.IsNullOrEmpty(label) ? id : label;
            Args = args;
        }

        /// <summary>What the game opens.</summary>
        public string Id { get; }

        /// <summary>What the Manager shows.</summary>
        public string Label { get; }

        /// <summary>Arguments for the game, or null.</summary>
        public string Args { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Label;
        }
    }
}
