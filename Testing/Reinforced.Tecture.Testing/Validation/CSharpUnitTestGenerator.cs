﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Reinforced.Tecture.Commands;
using Reinforced.Tecture.Testing.Generation;
using static Reinforced.Tecture.Testing.Validation.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Reinforced.Tecture.Testing.Validation
{
    partial class CSharpUnitTestGenerator : IGenerating, IValidationGenerator
    {
        private const string StoryVariableId = "flow";
        private readonly string TestClassName;
        private readonly string TestNamespaceName;

        private Queue<StatementSyntax> _chain;
        private HashSet<string> _usings;
        private HashSet<string> _staticUsings;

        public CSharpUnitTestGenerator(string testClassName, string testNamespaceName)
        {
            TestClassName = testClassName;
            TestNamespaceName = testNamespaceName;
        }
        private void EnsureUsing(string s)
        {
            if (!_usings.Contains(s)) _usings.Add(s);
        }
        private void EnsureUsing(Type t)
        {
            EnsureUsing(t.Namespace);
        }

        private void EnsureUsingStatic(MethodInfo mi)
        {
            if (mi.DeclaringType==null) return;
            var fullName = $"{mi.DeclaringType.Namespace}.{mi.DeclaringType.Name}";
            if (!_staticUsings.Contains(fullName)) _staticUsings.Add(fullName);
        }

        internal void Before()
        {
            _chain = new Queue<StatementSyntax>();
            _usings = new HashSet<string>();
            _staticUsings = new HashSet<string>();
        }



        public void Visit(CommandBase command, CheckDescription[] checks)
        {
            if (checks.Length == 0)
            {
                Skip();
            }
            else
            {
                var validationCalls = new List<InvocationExpressionSyntax>();
                foreach (var checkMethodDescription in checks)
                {
                    validationCalls.Add(Generate(command, checkMethodDescription));
                }
                Then(command, validationCalls);
            }
        }

        private void Then(CommandBase command, List<InvocationExpressionSyntax> checks)
        {
            var ex = InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(StoryVariableId),
                            NameThenOf(command)))
                .WithArgumentList(FormatThenArguments(checks));
            _chain.Enqueue(ExpressionStatement(ex).WithTrailingTrivia(LineFeed).WithLeadingTrivia(Formats.Tabs(4)));
        }

        private GenericNameSyntax NameThenOf(CommandBase command)
        {
            var type = command.GetType();
            EnsureUsing(type);
            var tn = type.TypeName(_usings);
            return GenericName(Identifier(nameof(TraceValidator.Then)))
                .WithTypeArgumentList(
                    TypeArgumentList(
                        SingletonSeparatedList(tn)));
        }



        private ArgumentListSyntax FormatThenArguments(List<InvocationExpressionSyntax> checks)
        {
            var args = ComaSeparatedChecks(checks);
            var arglist = ArgumentList(SeparatedList<ArgumentSyntax>(args.ToArray()));
            if (checks.Count > 1)
            {
                //wrap into multiple lines if we have several checks
                arglist = arglist.WithOpenParenToken(
                        Token(
                            Formats.BrWith4Tabs(),
                            SyntaxKind.OpenParenToken,
                            Formats.BrWith5Tabs()))
                    .WithCloseParenToken(
                        Token(
                            Formats.BrWith4Tabs(),
                            SyntaxKind.CloseParenToken,
                            TriviaList()));
            }

            return arglist;
        }

        private IEnumerable<SyntaxNodeOrToken> ComaSeparatedChecks(IEnumerable<InvocationExpressionSyntax> validationCalls)
        {
            bool first = true;
            foreach (var validationCall in validationCalls)
            {
                if (!first) yield return Token(SyntaxKind.CommaToken).BrWith5Tabs();
                yield return Argument(validationCall);
                first = false;
            }
        }


        private void Skip()
        {
            var ex = InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(StoryVariableId),
                            IdentifierName(nameof(TraceValidator.SomethingHappens)))
                )
                .WithTrailingTrivia(LineFeed);
            _chain.Enqueue(ExpressionStatement(ex).WithLeadingTrivia(Formats.Tabs(4)));
        }
    }
}