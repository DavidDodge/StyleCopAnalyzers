﻿// This file originally obtained from 
// https://raw.githubusercontent.com/code-cracker/code-cracker/08c1a01337964924eeed12be8b14c8ce8ec6b626/src/Common/CodeCracker.Common/Extensions/GeneratedCodeAnalysisExtensions.cs
// It is subject to the Apache License 2.0
// This file has been modified since obtaining it from its original source.

namespace StyleCop.Analyzers
{
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    internal static class GeneratedCodeAnalysisExtensions
    {
        /// <summary>
        /// A cache of the result of computing whether a document has an auto-generated header.
        /// </summary>
        /// <remarks>
        /// This allows many analyzers that run on every token in the file to avoid checking
        /// the same state in the document repeatedly.
        /// </remarks>
        private static readonly ConditionalWeakTable<SyntaxTree, StrongBox<bool?>> GeneratedHeaderPresentCheck
            = new ConditionalWeakTable<SyntaxTree, StrongBox<bool?>>();

        /// <summary>
        /// Checks whether the given node or its containing document is auto generated by a tool.
        /// </summary>
        internal static bool IsGenerated(this SyntaxNodeAnalysisContext context, CancellationToken cancellationToken)
        {
            return (context.Node.SyntaxTree?.IsGeneratedDocument(cancellationToken) ?? false) || (context.Node?.HasGeneratedAttribute() ?? false);
        }

        /// <summary>
        /// Checks whether the given document is auto generated by a tool.
        /// </summary>
        internal static bool IsGeneratedDocument(this SyntaxTreeAnalysisContext context, CancellationToken cancellationToken)
        {
            return context.Tree?.IsGeneratedDocument(cancellationToken) ?? false;
        }

        /// <summary>
        /// Checks whether the given symbol is auto generated by a tool.
        /// </summary>
        internal static bool IsGeneratedSymbol(this SymbolAnalysisContext context, CancellationToken cancellationToken)
        {
            if (context.Symbol == null) return false;
            foreach (var syntaxReference in context.Symbol.DeclaringSyntaxReferences)
            {
                if (syntaxReference.SyntaxTree.IsGeneratedDocument(cancellationToken)) return true;
                var root = syntaxReference.SyntaxTree.GetRoot(cancellationToken);
                var node = root?.FindNode(syntaxReference.Span);
                if (node.HasGeneratedAttribute()) return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether the given node has any of several attributes indicating it was generated by a tool.
        /// </summary>
        internal static bool HasGeneratedAttribute(this SyntaxNode node)
        {
            return node.HasAttributeOnAncestorOrSelf("GeneratedCode");
        }

        /// <summary>
        /// Checks whether the given document is auto generated by a tool
        /// (based on filename or comment header).
        /// </summary>
        internal static bool IsGeneratedDocument(this SyntaxTree tree, CancellationToken cancellationToken)
        {
            return (tree.FilePath?.IsGeneratedFileName() ?? false) || tree.HasAutoGeneratedComment(cancellationToken);
        }

        /// <summary>
        /// Checks whether the given document has an auto-generated comment as its header.
        /// </summary>
        internal static bool HasAutoGeneratedComment(this SyntaxTree tree, CancellationToken cancellationToken)
        {
            StrongBox<bool?> cachedResult = GeneratedHeaderPresentCheck.GetOrCreateValue(tree);
            if (cachedResult.Value.HasValue)
            {
                return cachedResult.Value.Value;
            }

            bool autoGenerated = HasAutoGeneratedCommentNoCache(tree, cancellationToken);

            // Update the strongbox's value with our computed result.
            // This doesn't change the strongbox reference, and its presence in the
            // ConditionalWeakTable is already assured, so we're updating in-place.
            // In the event of a race condition with another thread that set the value,
            // we'll just be re-setting it to the same value.
            cachedResult.Value = autoGenerated;

            return autoGenerated;
        }

        /// <summary>
        /// Checks whether the given document has an auto-generated comment as its header.
        /// </summary>
        private static bool HasAutoGeneratedCommentNoCache(SyntaxTree tree, CancellationToken cancellationToken)
        {
            var root = tree.GetRoot(cancellationToken);

            if (root == null) return false;
            var firstToken = root.GetFirstToken();
            SyntaxTriviaList trivia;
            if (firstToken == default(SyntaxToken))
            {
                var token = ((CompilationUnitSyntax)root).EndOfFileToken;
                if (!token.HasLeadingTrivia) return false;
                trivia = token.LeadingTrivia;
            }
            else
            {
                if (!firstToken.HasLeadingTrivia) return false;
                trivia = firstToken.LeadingTrivia;
            }

            var comments = trivia.Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));
            return comments.Any(t => t.ToString().Contains("<auto-generated"));
        }

        /// <summary>
        /// Checks whether the given document has a filename that indicates it is a generated file.
        /// </summary>
        private static bool IsGeneratedFileName(this string filePath)
        {
            return Regex.IsMatch(
                Path.GetFileName(filePath),
                @"(^service|^TemporaryGeneratedFile_.*|^assemblyinfo|^assemblyattributes|\.(g\.i|g|designer|generated|assemblyattributes))\.(cs|vb)$",
                RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
        }
    }
}
