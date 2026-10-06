using System;

namespace FutureDigitalPlus.UIKit.Lint
{
    /// <summary>One rule violation: which rule, where, and what is wrong. Pure data, shared by the Unity tests and the dotnet runner.</summary>
    public readonly struct LintFinding : IEquatable<LintFinding>
    {
        /// <summary>Short rule id, for example <c>uss-forbidden-property</c>.</summary>
        public string Rule { get; }

        /// <summary>Package relative path (forward slashes) or a type name.</summary>
        public string File { get; }

        /// <summary>1 based line, or 0 when the finding has no line.</summary>
        public int Line { get; }

        /// <summary>What is wrong, in one sentence.</summary>
        public string Message { get; }

        /// <summary>Creates a finding.</summary>
        public LintFinding(string rule, string file, int line, string message)
        {
            Rule = rule;
            File = file;
            Line = line;
            Message = message;
        }

        /// <inheritdoc />
        public bool Equals(LintFinding other) =>
            Rule == other.Rule && File == other.File && Line == other.Line && Message == other.Message;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is LintFinding other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (Rule, File, Line, Message).GetHashCode();

        /// <summary>The finding as <c>file:line: [rule] message</c>.</summary>
        public override string ToString() => Line > 0 ? $"{File}:{Line}: [{Rule}] {Message}" : $"{File}: [{Rule}] {Message}";
    }
}
