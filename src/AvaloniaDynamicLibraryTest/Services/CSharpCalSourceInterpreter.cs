using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaDynamicLibraryTest.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AvaloniaDynamicLibraryTest.Services;

public static class CSharpCalSourceInterpreter
{
    public static IReadOnlyList<LibraryInvocationResult> Invoke(
        string sourceCode,
        string sourceName,
        int left,
        int right)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            sourceCode,
            new CSharpParseOptions(LanguageVersion.Preview));

        var errors = syntaxTree.GetDiagnostics()
            .Where(x => x.Severity == DiagnosticSeverity.Error)
            .Select(x => x.ToString())
            .ToArray();
        if (errors.Length > 0)
        {
            return
            [
                new LibraryInvocationResult(sourceName, null, null, false, string.Join(Environment.NewLine, errors))
            ];
        }

        var root = syntaxTree.GetCompilationUnitRoot();
        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(IsCalMethod)
            .ToArray();

        if (methods.Length == 0)
        {
            return
            [
                new LibraryInvocationResult(
                    sourceName,
                    null,
                    null,
                    false,
                    "未找到 int Cal(int, int) 方法。")
            ];
        }

        var results = new List<LibraryInvocationResult>();
        foreach (var method in methods)
        {
            var typeName = GetTypeName(method);
            try
            {
                var value = new MethodEvaluator(method, left, right).Execute();
                results.Add(new LibraryInvocationResult(sourceName, typeName, value, true, "执行成功。"));
            }
            catch (Exception ex)
            {
                results.Add(new LibraryInvocationResult(sourceName, typeName, null, false, ex.Message));
            }
        }

        return results;
    }

    private static bool IsCalMethod(MethodDeclarationSyntax method)
    {
        if (!string.Equals(method.Identifier.ValueText, "Cal", StringComparison.Ordinal))
        {
            return false;
        }

        if (!IsIntType(method.ReturnType))
        {
            return false;
        }

        var parameters = method.ParameterList.Parameters;
        return parameters.Count == 2 &&
               parameters.All(parameter => parameter.Type is not null && IsIntType(parameter.Type));
    }

    private static bool IsIntType(TypeSyntax type)
    {
        var text = type.ToString();
        return string.Equals(text, "int", StringComparison.Ordinal) ||
               string.Equals(text, "Int32", StringComparison.Ordinal) ||
               string.Equals(text, "System.Int32", StringComparison.Ordinal);
    }

    private static string GetTypeName(SyntaxNode node)
    {
        var typeNames = node.Ancestors()
            .OfType<TypeDeclarationSyntax>()
            .Reverse()
            .Select(type => type.Identifier.ValueText)
            .ToArray();

        var typeName = typeNames.Length == 0 ? "<global>" : string.Join(".", typeNames);
        var namespaceName = node.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault()
            ?.Name
            .ToString();

        return string.IsNullOrWhiteSpace(namespaceName)
            ? typeName
            : namespaceName + "." + typeName;
    }

    private sealed class MethodEvaluator
    {
        private const int MaxLoopIterations = 1_000_000;
        private readonly MethodDeclarationSyntax _method;
        private readonly Dictionary<string, int> _variables = new(StringComparer.Ordinal);

        public MethodEvaluator(MethodDeclarationSyntax method, int left, int right)
        {
            _method = method;

            var parameters = method.ParameterList.Parameters;
            _variables[parameters[0].Identifier.ValueText] = left;
            _variables[parameters[1].Identifier.ValueText] = right;
        }

        public int Execute()
        {
            if (_method.ExpressionBody is not null)
            {
                return EvaluateInt(_method.ExpressionBody.Expression);
            }

            if (_method.Body is null)
            {
                throw new NotSupportedException("Cal 方法没有可执行方法体。");
            }

            var result = ExecuteStatements(_method.Body.Statements);
            if (!result.Returned)
            {
                throw new NotSupportedException("Cal 方法没有返回 int。");
            }

            return result.Value;
        }

        private (bool Returned, int Value) ExecuteStatements(IEnumerable<StatementSyntax> statements)
        {
            foreach (var statement in statements)
            {
                var result = ExecuteStatement(statement);
                if (result.Returned)
                {
                    return result;
                }
            }

            return (false, 0);
        }

        private (bool Returned, int Value) ExecuteStatement(StatementSyntax statement)
        {
            switch (statement)
            {
                case BlockSyntax block:
                    return ExecuteStatements(block.Statements);

                case LocalDeclarationStatementSyntax localDeclaration:
                    ExecuteLocalDeclaration(localDeclaration);
                    return (false, 0);

                case ExpressionStatementSyntax expressionStatement:
                    EvaluateInt(expressionStatement.Expression);
                    return (false, 0);

                case ReturnStatementSyntax returnStatement:
                    if (returnStatement.Expression is null)
                    {
                        throw new NotSupportedException("return 语句必须返回 int。");
                    }

                    return (true, EvaluateInt(returnStatement.Expression));

                case WhileStatementSyntax whileStatement:
                    return ExecuteWhile(whileStatement);

                case ForStatementSyntax forStatement:
                    return ExecuteFor(forStatement);

                case IfStatementSyntax ifStatement:
                    return EvaluateBool(ifStatement.Condition)
                        ? ExecuteStatement(ifStatement.Statement)
                        : ifStatement.Else is null
                            ? (false, 0)
                            : ExecuteStatement(ifStatement.Else.Statement);

                case CheckedStatementSyntax checkedStatement:
                    return ExecuteStatement(checkedStatement.Block);

                case EmptyStatementSyntax:
                    return (false, 0);

                default:
                    throw new NotSupportedException($"暂不支持语句：{statement.Kind()}。");
            }
        }

        private void ExecuteLocalDeclaration(LocalDeclarationStatementSyntax localDeclaration)
        {
            foreach (var variable in localDeclaration.Declaration.Variables)
            {
                _variables[variable.Identifier.ValueText] = variable.Initializer is null
                    ? 0
                    : EvaluateInt(variable.Initializer.Value);
            }
        }

        private (bool Returned, int Value) ExecuteWhile(WhileStatementSyntax whileStatement)
        {
            var iterations = 0;
            while (EvaluateBool(whileStatement.Condition))
            {
                if (++iterations > MaxLoopIterations)
                {
                    throw new InvalidOperationException("循环次数超过限制。");
                }

                var result = ExecuteStatement(whileStatement.Statement);
                if (result.Returned)
                {
                    return result;
                }
            }

            return (false, 0);
        }

        private (bool Returned, int Value) ExecuteFor(ForStatementSyntax forStatement)
        {
            if (forStatement.Declaration is not null)
            {
                foreach (var variable in forStatement.Declaration.Variables)
                {
                    _variables[variable.Identifier.ValueText] = variable.Initializer is null
                        ? 0
                        : EvaluateInt(variable.Initializer.Value);
                }
            }

            foreach (var initializer in forStatement.Initializers)
            {
                EvaluateInt(initializer);
            }

            var iterations = 0;
            while (forStatement.Condition is null || EvaluateBool(forStatement.Condition))
            {
                if (++iterations > MaxLoopIterations)
                {
                    throw new InvalidOperationException("循环次数超过限制。");
                }

                var result = ExecuteStatement(forStatement.Statement);
                if (result.Returned)
                {
                    return result;
                }

                foreach (var incrementor in forStatement.Incrementors)
                {
                    EvaluateInt(incrementor);
                }
            }

            return (false, 0);
        }

        private bool EvaluateBool(ExpressionSyntax expression)
        {
            return expression switch
            {
                LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.TrueLiteralExpression) => true,
                LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.FalseLiteralExpression) => false,
                PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.LogicalNotExpression) =>
                    !EvaluateBool(prefix.Operand),
                BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression) =>
                    EvaluateBool(binary.Left) && EvaluateBool(binary.Right),
                BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalOrExpression) =>
                    EvaluateBool(binary.Left) || EvaluateBool(binary.Right),
                _ => EvaluateInt(expression) != 0
            };
        }

        private int EvaluateInt(ExpressionSyntax expression)
        {
            return expression switch
            {
                LiteralExpressionSyntax literal => EvaluateLiteral(literal),
                IdentifierNameSyntax identifier => GetVariable(identifier.Identifier.ValueText),
                ParenthesizedExpressionSyntax parenthesized => EvaluateInt(parenthesized.Expression),
                PrefixUnaryExpressionSyntax prefix => EvaluatePrefix(prefix),
                PostfixUnaryExpressionSyntax postfix => EvaluatePostfix(postfix),
                BinaryExpressionSyntax binary => EvaluateBinary(binary),
                AssignmentExpressionSyntax assignment => EvaluateAssignment(assignment),
                InvocationExpressionSyntax invocation => EvaluateInvocation(invocation),
                CastExpressionSyntax cast => EvaluateInt(cast.Expression),
                CheckedExpressionSyntax checkedExpression => EvaluateInt(checkedExpression.Expression),
                _ => throw new NotSupportedException($"暂不支持表达式：{expression.Kind()}。")
            };
        }

        private static int EvaluateLiteral(LiteralExpressionSyntax literal)
        {
            if (literal.IsKind(SyntaxKind.TrueLiteralExpression))
            {
                return 1;
            }

            if (literal.IsKind(SyntaxKind.FalseLiteralExpression))
            {
                return 0;
            }

            if (literal.Token.Value is int intValue)
            {
                return intValue;
            }

            throw new NotSupportedException($"暂不支持字面量：{literal.Token.Text}。");
        }

        private int EvaluatePrefix(PrefixUnaryExpressionSyntax prefix)
        {
            return prefix.Kind() switch
            {
                SyntaxKind.UnaryPlusExpression => EvaluateInt(prefix.Operand),
                SyntaxKind.UnaryMinusExpression => unchecked(-EvaluateInt(prefix.Operand)),
                SyntaxKind.BitwiseNotExpression => ~EvaluateInt(prefix.Operand),
                SyntaxKind.LogicalNotExpression => EvaluateBool(prefix.Operand) ? 0 : 1,
                SyntaxKind.PreIncrementExpression => Increment(prefix.Operand, 1, returnOriginal: false),
                SyntaxKind.PreDecrementExpression => Increment(prefix.Operand, -1, returnOriginal: false),
                _ => throw new NotSupportedException($"暂不支持前缀表达式：{prefix.Kind()}。")
            };
        }

        private int EvaluatePostfix(PostfixUnaryExpressionSyntax postfix)
        {
            return postfix.Kind() switch
            {
                SyntaxKind.PostIncrementExpression => Increment(postfix.Operand, 1, returnOriginal: true),
                SyntaxKind.PostDecrementExpression => Increment(postfix.Operand, -1, returnOriginal: true),
                _ => throw new NotSupportedException($"暂不支持后缀表达式：{postfix.Kind()}。")
            };
        }

        private int EvaluateBinary(BinaryExpressionSyntax binary)
        {
            if (binary.IsKind(SyntaxKind.LogicalAndExpression))
            {
                return EvaluateBool(binary.Left) && EvaluateBool(binary.Right) ? 1 : 0;
            }

            if (binary.IsKind(SyntaxKind.LogicalOrExpression))
            {
                return EvaluateBool(binary.Left) || EvaluateBool(binary.Right) ? 1 : 0;
            }

            var left = EvaluateInt(binary.Left);
            var right = EvaluateInt(binary.Right);

            return binary.Kind() switch
            {
                SyntaxKind.AddExpression => unchecked(left + right),
                SyntaxKind.SubtractExpression => unchecked(left - right),
                SyntaxKind.MultiplyExpression => unchecked(left * right),
                SyntaxKind.DivideExpression => left / right,
                SyntaxKind.ModuloExpression => left % right,
                SyntaxKind.LeftShiftExpression => left << right,
                SyntaxKind.RightShiftExpression => left >> right,
                SyntaxKind.BitwiseAndExpression => left & right,
                SyntaxKind.BitwiseOrExpression => left | right,
                SyntaxKind.ExclusiveOrExpression => left ^ right,
                SyntaxKind.EqualsExpression => left == right ? 1 : 0,
                SyntaxKind.NotEqualsExpression => left != right ? 1 : 0,
                SyntaxKind.LessThanExpression => left < right ? 1 : 0,
                SyntaxKind.LessThanOrEqualExpression => left <= right ? 1 : 0,
                SyntaxKind.GreaterThanExpression => left > right ? 1 : 0,
                SyntaxKind.GreaterThanOrEqualExpression => left >= right ? 1 : 0,
                _ => throw new NotSupportedException($"暂不支持二元表达式：{binary.Kind()}。")
            };
        }

        private int EvaluateAssignment(AssignmentExpressionSyntax assignment)
        {
            var variableName = GetAssignableName(assignment.Left);
            var currentValue = GetVariable(variableName);
            var rightValue = EvaluateInt(assignment.Right);

            var value = assignment.Kind() switch
            {
                SyntaxKind.SimpleAssignmentExpression => rightValue,
                SyntaxKind.AddAssignmentExpression => unchecked(currentValue + rightValue),
                SyntaxKind.SubtractAssignmentExpression => unchecked(currentValue - rightValue),
                SyntaxKind.MultiplyAssignmentExpression => unchecked(currentValue * rightValue),
                SyntaxKind.DivideAssignmentExpression => currentValue / rightValue,
                SyntaxKind.ModuloAssignmentExpression => currentValue % rightValue,
                SyntaxKind.AndAssignmentExpression => currentValue & rightValue,
                SyntaxKind.OrAssignmentExpression => currentValue | rightValue,
                SyntaxKind.ExclusiveOrAssignmentExpression => currentValue ^ rightValue,
                SyntaxKind.LeftShiftAssignmentExpression => currentValue << rightValue,
                SyntaxKind.RightShiftAssignmentExpression => currentValue >> rightValue,
                _ => throw new NotSupportedException($"暂不支持赋值表达式：{assignment.Kind()}。")
            };

            _variables[variableName] = value;
            return value;
        }

        private int EvaluateInvocation(InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                throw new NotSupportedException($"暂不支持方法调用：{invocation.Expression}。");
            }

            var target = memberAccess.Expression.ToString();
            if (!string.Equals(target, "Math", StringComparison.Ordinal) &&
                !string.Equals(target, "System.Math", StringComparison.Ordinal))
            {
                throw new NotSupportedException($"暂不支持方法调用：{memberAccess}。");
            }

            var args = invocation.ArgumentList.Arguments
                .Select(argument => EvaluateInt(argument.Expression))
                .ToArray();

            return memberAccess.Name.Identifier.ValueText switch
            {
                "Abs" when args.Length == 1 => Math.Abs(args[0]),
                "Min" when args.Length == 2 => Math.Min(args[0], args[1]),
                "Max" when args.Length == 2 => Math.Max(args[0], args[1]),
                "Clamp" when args.Length == 3 => Math.Clamp(args[0], args[1], args[2]),
                _ => throw new NotSupportedException($"暂不支持 Math 方法：{memberAccess.Name}。")
            };
        }

        private int Increment(ExpressionSyntax expression, int delta, bool returnOriginal)
        {
            var variableName = GetAssignableName(expression);
            var currentValue = GetVariable(variableName);
            var nextValue = unchecked(currentValue + delta);
            _variables[variableName] = nextValue;
            return returnOriginal ? currentValue : nextValue;
        }

        private int GetVariable(string name)
        {
            if (_variables.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new InvalidOperationException($"变量未定义：{name}。");
        }

        private static string GetAssignableName(ExpressionSyntax expression)
        {
            return expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                ParenthesizedExpressionSyntax parenthesized => GetAssignableName(parenthesized.Expression),
                _ => throw new NotSupportedException($"暂不支持赋值目标：{expression.Kind()}。")
            };
        }
    }
}
