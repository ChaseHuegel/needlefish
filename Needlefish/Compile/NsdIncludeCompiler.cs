using Needlefish.Lexing;
using Needlefish.Linting;
using Needlefish.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Needlefish.Compile;

/// <summary>
/// Loads an nsd document together with its transitive includes and resolves the document's field
/// types against the types the includes make visible. Each document is compiled on its own;
/// includes provide type visibility only, they do not merge output.
/// </summary>
internal static class NsdIncludeCompiler
{
    public static NsdIncludeResult Prepare(
        string filePath,
        string source,
        INsdFileResolver? resolver,
        IReadOnlyCollection<string>? knownRoots)
    {
        var graph = new IncludeGraph(resolver, knownRoots);
        return graph.Prepare(filePath, source);
    }

    /// <summary>
    /// The resolved root document and the diagnostics found while loading and validating its
    /// include closure. <see cref="Root"/> is <c>null</c> when the document cannot be compiled.
    /// </summary>
    internal readonly struct NsdIncludeResult
    {
        public Nsd? Root { get; }

        public NsdDiagnostic[] Diagnostics { get; }

        public NsdIncludeResult(Nsd? root, NsdDiagnostic[] diagnostics)
        {
            Root = root;
            Diagnostics = diagnostics;
        }
    }

    private sealed class IncludeGraph
    {
        private readonly INsdFileResolver? _resolver;
        private readonly HashSet<string> _knownRoots;
        private readonly Dictionary<string, Node> _nodes;
        private readonly List<Node> _stack = new();
        private readonly List<Node> _loadOrder = new();
        private readonly Lexer<TokenType> _lexer = NsdLexer.Create();
        private readonly NsdParser _parser = new();

        public IncludeGraph(INsdFileResolver? resolver, IReadOnlyCollection<string>? knownRoots)
        {
            _resolver = resolver;
            _knownRoots = new HashSet<string>(NsdPaths.Comparer);

            if (knownRoots != null)
            {
                foreach (string root in knownRoots)
                {
                    _knownRoots.Add(NsdPaths.Normalize(root));
                }
            }

            _nodes = new Dictionary<string, Node>(NsdPaths.Comparer);
        }

        public NsdIncludeResult Prepare(string filePath, string source)
        {
            Node root = Load(NsdPaths.Normalize(filePath), source, isRoot: true);

            foreach (Node node in _loadOrder)
            {
                ResolveNode(node);
            }

            var diagnostics = new List<NsdDiagnostic>();
            diagnostics.AddRange(root.Diagnostics);

            foreach (Node node in GetClosure(root))
            {
                if (node != root && !node.IsKnownRoot)
                {
                    diagnostics.AddRange(node.Diagnostics);
                }
            }

            return root.Valid && root.Raw != null
                ? new NsdIncludeResult(root.ToNsd(), diagnostics.ToArray())
                : new NsdIncludeResult(null, diagnostics.ToArray());
        }

        private Node Load(string path, string source, bool isRoot)
        {
            if (_nodes.TryGetValue(path, out Node? existing))
            {
                return existing;
            }

            var node = new Node(path, isRoot || _knownRoots.Contains(path));
            _nodes.Add(path, node);
            _stack.Add(node);

            try
            {
                List<Token<TokenType>> tokens = _lexer.Lex(source);

                if (tokens.Count == 0)
                {
                    AddError(node, "The nsd document is empty.");
                }
                else
                {
                    node.Raw = _parser.ParseRaw(tokens);
                }
            }
            catch (LexException lexException)
            {
                AddError(node, lexException.Message, lexException.Position, lexException.Length);
            }
            catch (NsdException nsdException)
            {
                AddError(node, nsdException.Message, nsdException.Position, nsdException.Length);
            }
            catch (FormatException formatException)
            {
                AddError(node, formatException.Message);
            }

            if (node.Raw != null)
            {
                LoadIncludes(node);
            }

            _stack.RemoveAt(_stack.Count - 1);
            _loadOrder.Add(node);
            return node;
        }

        private void LoadIncludes(Node node)
        {
            foreach (Define define in node.Raw!.Value.Defines)
            {
                if (define.Key != "include")
                {
                    continue;
                }

                var edge = new IncludeEdge(define.Value, define.Position, define.Length);
                node.Includes.Add(edge);

                if (_resolver == null)
                {
                    edge.Failure = string.Format(
                        "Included file '{0}' cannot be resolved because no file resolver is configured.",
                        define.Value);
                    AddError(node, edge.Failure, edge.Position, edge.Length);
                    continue;
                }

                NsdFileResolution resolution = _resolver.Resolve(node.Path, define.Value);
                string targetPath = NsdPaths.Normalize(resolution.Path);

                if (!resolution.Success)
                {
                    edge.Failure = resolution.Error ?? string.Format(
                        "Included file '{0}' could not be resolved.",
                        define.Value);
                    AddError(node, edge.Failure, edge.Position, edge.Length);
                    continue;
                }

                int cycleIndex = IndexOnStack(targetPath);
                if (cycleIndex >= 0)
                {
                    var chain = new List<string>();
                    for (int i = cycleIndex; i < _stack.Count; i++)
                    {
                        chain.Add(DisplayPath(_stack[i].Path));
                    }
                    chain.Add(DisplayPath(targetPath));

                    edge.CycleChain = string.Join(" -> ", chain);
                    string message = string.Format("Include cycle detected: {0}.", edge.CycleChain);

                    for (int i = cycleIndex; i < _stack.Count; i++)
                    {
                        Node member = _stack[i];
                        member.Valid = false;
                        member.ErrorSummary ??= message;
                    }

                    continue;
                }

                if (_nodes.TryGetValue(targetPath, out Node? target))
                {
                    edge.Target = target;
                }
                else
                {
                    edge.Target = Load(targetPath, resolution.Content!, isRoot: false);
                }
            }
        }

        private void ResolveNode(Node node)
        {
            foreach (IncludeEdge edge in node.Includes)
            {
                if (edge.CycleChain != null)
                {
                    AddError(node, string.Format("Include cycle detected: {0}.", edge.CycleChain), edge.Position, edge.Length);
                }
            }

            if (node.Raw == null)
            {
                return;
            }

            List<Node> closure = GetClosure(node);

            TypeDefinition[] visibleTypes = closure
                .Where(file => file.Raw != null)
                .SelectMany(file => file.Raw!.Value.TypeDefinitions)
                .ToArray();

            var issues = new List<Issue>();
            node.ResolvedTypes = NsdProcessor.ResolveTypeDefinitions(
                node.Raw.Value.TypeDefinitions,
                visibleTypes,
                GetNamespace(node.Raw.Value),
                issues);

            issues.AddRange(NsdLinter.Lint(node.Raw.Value.Defines, node.ResolvedTypes));

            foreach (Issue issue in issues)
            {
                var diagnostic = new NsdDiagnostic(
                    issue.Message,
                    issue.Level == IssueLevel.Error ? NsdDiagnosticLevel.Error : NsdDiagnosticLevel.Warning,
                    issue.Position,
                    issue.Length,
                    node.Path);

                if (issue.Level == IssueLevel.Error)
                {
                    AddError(node, diagnostic);
                }
                else
                {
                    node.Diagnostics.Add(diagnostic);
                }
            }

            DetectDuplicateTypes(node, closure);
            ValidateIncludes(node);
        }

        private void ValidateIncludes(Node node)
        {
            foreach (IncludeEdge edge in node.Includes)
            {
                if (edge.Target == null || edge.Failure != null)
                {
                    continue;
                }

                if (!edge.Target.Valid)
                {
                    AddError(node, IncludeErrorSummary(edge.Target), edge.Position, edge.Length);
                    continue;
                }

                if (_knownRoots.Count > 0 && !edge.Target.IsKnownRoot)
                {
                    node.Diagnostics.Add(new NsdDiagnostic(
                        string.Format(
                            "Included file '{0}' is not part of the compilation and will not be generated.",
                            edge.Path),
                        NsdDiagnosticLevel.Warning,
                        edge.Position,
                        edge.Length,
                        node.Path));
                }
            }
        }

        private void DetectDuplicateTypes(Node node, List<Node> closure)
        {
            var owners = new Dictionary<(string Namespace, string Name), Node>();

            foreach (Node file in closure)
            {
                if (file.Raw == null)
                {
                    continue;
                }

                foreach (TypeDefinition type in file.Raw.Value.TypeDefinitions)
                {
                    if (owners.TryGetValue((type.Namespace, type.Name), out Node? first))
                    {
                        // Duplicates within a single document are reported by the linter for that
                        // document, so only cross document duplicates are handled here.
                        if (first == file)
                        {
                            continue;
                        }

                        string message = string.Format(
                            "Duplicate type ({0}). Declared in '{1}' and '{2}'.",
                            type.GetQualifiedName(),
                            DisplayPath(first.Path),
                            DisplayPath(file.Path));

                        if (file == node || first == node)
                        {
                            TypeDefinition ownType = file == node ? type : FindOwnType(node, type.Name);
                            AddError(node, message, ownType.Position, ownType.Length);
                        }
                        else
                        {
                            IncludeEdge? edge = FindIncludeEdge(node, file);
                            AddError(node, message, edge?.Position ?? -1, edge?.Length ?? 0);
                        }
                    }
                    else
                    {
                        owners.Add((type.Namespace, type.Name), file);
                    }
                }
            }
        }

        private List<Node> GetClosure(Node node)
        {
            if (node.Closure != null)
            {
                return node.Closure;
            }

            var closure = new List<Node> { node };
            var seen = new HashSet<string>(NsdPaths.Comparer) { node.Path };

            foreach (IncludeEdge edge in node.Includes)
            {
                if (edge.Target == null)
                {
                    continue;
                }

                foreach (Node included in GetClosure(edge.Target))
                {
                    if (seen.Add(included.Path))
                    {
                        closure.Add(included);
                    }
                }
            }

            node.Closure = closure;
            return closure;
        }

        private static IncludeEdge? FindIncludeEdge(Node node, Node target)
        {
            foreach (IncludeEdge edge in node.Includes)
            {
                if (edge.Target == target || (edge.Target?.Closure?.Contains(target) ?? false))
                {
                    return edge;
                }
            }

            return null;
        }

        private static TypeDefinition FindOwnType(Node node, string name)
        {
            if (node.Raw != null)
            {
                foreach (TypeDefinition type in node.Raw.Value.TypeDefinitions)
                {
                    if (string.Equals(type.Name, name, StringComparison.Ordinal))
                    {
                        return type;
                    }
                }
            }

            return default;
        }

        private int IndexOnStack(string path)
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                if (string.Equals(_stack[i].Path, path, NsdPaths.Comparison))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string GetNamespace(Nsd nsd)
        {
            Define namespaceDefine = nsd.Defines.FirstOrDefault(define => define.Key == "namespace");
            return namespaceDefine.Value ?? string.Empty;
        }

        private static string IncludeErrorSummary(Node target)
        {
            string name = DisplayPath(target.Path);
            return target.ErrorSummary == null
                ? string.Format("Included file '{0}' contains errors.", name)
                : string.Format("Included file '{0}' contains errors: {1}", name, target.ErrorSummary);
        }

        private static string DisplayPath(string path)
        {
            string name = Path.GetFileName(path);
            return string.IsNullOrEmpty(name) ? path : name;
        }

        private static void AddError(Node node, string message, int position = -1, int length = 0)
        {
            AddError(node, new NsdDiagnostic(message, NsdDiagnosticLevel.Error, position, length, node.Path));
        }

        private static void AddError(Node node, NsdDiagnostic diagnostic)
        {
            node.Diagnostics.Add(diagnostic);
            node.Valid = false;
            node.ErrorSummary ??= diagnostic.Message;
        }

        private sealed class Node
        {
            public Node(string path, bool isKnownRoot)
            {
                Path = path;
                IsKnownRoot = isKnownRoot;
            }

            public string Path { get; }

            public bool IsKnownRoot { get; }

            public Nsd? Raw { get; set; }

            public List<IncludeEdge> Includes { get; } = new();

            public List<Node>? Closure { get; set; }

            public TypeDefinition[] ResolvedTypes { get; set; } = Array.Empty<TypeDefinition>();

            public List<NsdDiagnostic> Diagnostics { get; } = new();

            public bool Valid { get; set; } = true;

            public string? ErrorSummary { get; set; }

            public Nsd ToNsd()
            {
                return new Nsd(Raw!.Value.Version, Raw.Value.Defines, ResolvedTypes);
            }
        }

        private sealed class IncludeEdge
        {
            public IncludeEdge(string path, int position, int length)
            {
                Path = path;
                Position = position;
                Length = length;
            }

            public string Path { get; }

            public int Position { get; }

            public int Length { get; }

            public Node? Target { get; set; }

            public string? Failure { get; set; }

            public string? CycleChain { get; set; }
        }
    }
}
