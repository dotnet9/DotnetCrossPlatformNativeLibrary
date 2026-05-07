using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AvaloniaEncryptedCSharpFileTest.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AvaloniaEncryptedCSharpFileTest.Services;

public static class CSharpCalInterpreter
{
    private const int MaxLoopIterations = 1_000_000;

    public static IReadOnlyList<SourceExecutionResult> Execute(
        string displayName,
        string sourceCode,
        int left,
        int right,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(
                SourceText.From(sourceCode),
                new CSharpParseOptions(LanguageVersion.Preview),
                path: displayName,
                cancellationToken: cancellationToken);

            var errors = syntaxTree.GetDiagnostics(cancellationToken)
                .Where(x => x.Severity == DiagnosticSeverity.Error)
                .Select(x => x.ToString())
                .ToArray();
            if (errors.Length > 0)
            {
                return [new SourceExecutionResult(displayName, null, null, false, string.Join(Environment.NewLine, errors))];
            }

            var root = syntaxTree.GetCompilationUnitRoot(cancellationToken);
            var methods = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(IsCalMethod)
                .ToArray();

            if (methods.Length == 0)
            {
                return [new SourceExecutionResult(displayName, null, null, false, "未找到公开的 int Cal(int, int) 方法。")];
            }

            return methods
                .Select(method => ExecuteMethod(displayName, method, left, right, cancellationToken))
                .ToArray();
        }
        catch (Exception ex)
        {
            return [new SourceExecutionResult(displayName, null, null, false, ex.Message)];
        }
    }

    private static SourceExecutionResult ExecuteMethod(
        string displayName,
        MethodDeclarationSyntax method,
        int left,
        int right,
        CancellationToken cancellationToken)
    {
        var typeName = GetTypeName(method);
        try
        {
            var variables = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["left"] = left,
                ["right"] = right
            };

            if (method.ExpressionBody is not null)
            {
                var expressionResult = EvaluateInt(method.ExpressionBody.Expression, variables);
                return new SourceExecutionResult(displayName, typeName, expressionResult, true, "解释执行成功。");
            }

            if (method.Body is null)
            {
                return new SourceExecutionResult(displayName, typeName, null, false, "Cal 方法没有可执行方法体。");
            }

            var flow = ExecuteBlock(method.Body, variables, cancellationToken);
            if (!flow.HasReturned)
            {
                return new SourceExecutionResult(displayName, typeName, null, false, "Cal 方法没有返回值。");
            }

            return new SourceExecutionResult(displayName, typeName, flow.ReturnValue, true, "解释执行成功。");
        }
        catch (Exception ex)
        {
            return new SourceExecutionResult(displayName, typeName, null, false, ex.Message);
        }
    }

    private static ExecutionFlow ExecuteBlock(
        BlockSyntax block,
        Dictionary<string, int> variables,
        CancellationToken cancellationToken)
    {
        return ExecuteStatements(block.Statements, variables, cancellationToken);
    }

    private static ExecutionFlow ExecuteStatements(
        SyntaxList<StatementSyntax> statements,
        Dictionary<string, int> variables,
        CancellationToken cancellationToken)
    {
        foreach (var statement in statements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var flow = ExecuteStatement(statement, variables, cancellationToken);
            if (flow.HasReturned)
            {
                return flow;
            }
        }

        return ExecutionFlow.None;
    }

    private static ExecutionFlow ExecuteStatement(
        StatementSyntax statement,
        Dictionary<string, int> variables,
        CancellationToken cancellationToken)
    {
        switch (statement)
        {
            case BlockSyntax block:
                return ExecuteBlock(block, variables, cancellationToken);
            case LocalDeclarationStatementSyntax localDeclaration:
                ExecuteLocalDeclaration(localDeclaration, variables);
                return ExecutionFlow.None;
            case ExpressionStatementSyntax expressionStatement:
                _ = EvaluateInt(expressionStatement.Expression, variables);
                return ExecutionFlow.None;
            case ReturnStatementSyntax returnStatement:
                if (returnStatement.Expression is null)
                {
                    throw new NotSupportedException("Cal 方法必须返回 int。");
                }

                return ExecutionFlow.Return(EvaluateInt(returnStatement.Expression, variables));
            case WhileStatementSyntax whileStatement:
                return ExecuteWhile(whileStatement, variables, cancellationToken);
            case IfStatementSyntax ifStatement:
                return ExecuteIf(ifStatement, variables, cancellationToken);
            case CheckedStatementSyntax checkedStatement:
                return ExecuteBlock(checkedStatement.Block, variables, cancellationToken);
            case EmptyStatementSyntax:
                return ExecutionFlow.None;
            default:
                throw new NotSupportedException($"AOT 解释器暂不支持语句：{statement.Kind()}。");
        }
    }

    private static void ExecuteLocalDeclaration(
        LocalDeclarationStatementSyntax localDeclaration,
        Dictionary<string, int> variables)
    {
        foreach (var variable in localDeclaration.Declaration.Variables)
        {
            if (variable.Initializer is null)
            {
                throw new NotSupportedException($"变量 {variable.Identifier.Text} 必须初始化。");
            }

            variables[variable.Identifier.Text] = EvaluateInt(variable.Initializer.Value, variables);
        }
    }

    private static ExecutionFlow ExecuteWhile(
        WhileStatementSyntax whileStatement,
        Dictionary<string, int> variables,
        CancellationToken cancellationToken)
    {
        var iterations = 0;
        while (EvaluateBool(whileStatement.Condition, variables))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (++iterations > MaxLoopIterations)
            {
                throw new InvalidOperationException("while 循环超过安全上限，已停止执行。");
            }

            var flow = ExecuteStatement(whileStatement.Statement, variables, cancellationToken);
            if (flow.HasReturned)
            {
                return flow;
            }
        }

        return ExecutionFlow.None;
    }

    private static ExecutionFlow ExecuteIf(
        IfStatementSyntax ifStatement,
        Dictionary<string, int> variables,
        CancellationToken cancellationToken)
    {
        if (EvaluateBool(ifStatement.Condition, variables))
        {
            return ExecuteStatement(ifStatement.Statement, variables, cancellationToken);
        }

        return ifStatement.Else is null
            ? ExecutionFlow.None
            : ExecuteStatement(ifStatement.Else.Statement, variables, cancellationToken);
    }

    private static int EvaluateInt(ExpressionSyntax expression, Dictionary<string, int> variables)
    {
        unchecked
        {
            return expression switch
            {
                LiteralExpressionSyntax literal => EvaluateLiteral(literal),
                IdentifierNameSyntax identifier => GetVariable(variables, identifier.Identifier.Text),
                ParenthesizedExpressionSyntax parenthesized => EvaluateInt(parenthesized.Expression, variables),
                PrefixUnaryExpressionSyntax prefix => EvaluatePrefixUnary(prefix, variables),
                PostfixUnaryExpressionSyntax postfix => EvaluatePostfixUnary(postfix, variables),
                BinaryExpressionSyntax binary => EvaluateBinary(binary, variables),
                AssignmentExpressionSyntax assignment => EvaluateAssignment(assignment, variables),
                InvocationExpressionSyntax invocation => EvaluateInvocation(invocation, variables),
                CastExpressionSyntax cast => EvaluateInt(cast.Expression, variables),
                ConditionalExpressionSyntax conditional => EvaluateBool(conditional.Condition, variables)
                    ? EvaluateInt(conditional.WhenTrue, variables)
                    : EvaluateInt(conditional.WhenFalse, variables),
                _ => throw new NotSupportedException($"AOT 解释器暂不支持表达式：{expression.Kind()}。")
            };
        }
    }

    private static bool EvaluateBool(ExpressionSyntax expression, Dictionary<string, int> variables)
    {
        return expression switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.TrueLiteralExpression) => true,
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.FalseLiteralExpression) => false,
            PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.LogicalNotExpression) =>
                !EvaluateBool(prefix.Operand, variables),
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression) =>
                EvaluateBool(binary.Left, variables) && EvaluateBool(binary.Right, variables),
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalOrExpression) =>
                EvaluateBool(binary.Left, variables) || EvaluateBool(binary.Right, variables),
            BinaryExpressionSyntax binary => EvaluateComparison(binary, variables),
            ParenthesizedExpressionSyntax parenthesized => EvaluateBool(parenthesized.Expression, variables),
            _ => EvaluateInt(expression, variables) != 0
        };
    }

    private static int EvaluateLiteral(LiteralExpressionSyntax literal)
    {
        if (literal.IsKind(SyntaxKind.NumericLiteralExpression) && literal.Token.Value is not null)
        {
            return Convert.ToInt32(literal.Token.Value);
        }

        if (literal.IsKind(SyntaxKind.TrueLiteralExpression))
        {
            return 1;
        }

        if (literal.IsKind(SyntaxKind.FalseLiteralExpression))
        {
            return 0;
        }

        throw new NotSupportedException($"AOT 解释器暂不支持字面量：{literal}。");
    }

    private static int EvaluatePrefixUnary(
        PrefixUnaryExpressionSyntax prefix,
        Dictionary<string, int> variables)
    {
        unchecked
        {
            if (prefix.IsKind(SyntaxKind.UnaryMinusExpression))
            {
                return -EvaluateInt(prefix.Operand, variables);
            }

            if (prefix.IsKind(SyntaxKind.UnaryPlusExpression))
            {
                return EvaluateInt(prefix.Operand, variables);
            }

            if (prefix.IsKind(SyntaxKind.BitwiseNotExpression))
            {
                return ~EvaluateInt(prefix.Operand, variables);
            }

            if (prefix.IsKind(SyntaxKind.PreIncrementExpression))
            {
                return IncrementVariable(prefix.Operand, variables, 1);
            }

            if (prefix.IsKind(SyntaxKind.PreDecrementExpression))
            {
                return IncrementVariable(prefix.Operand, variables, -1);
            }

            throw new NotSupportedException($"AOT 解释器暂不支持一元表达式：{prefix.Kind()}。");
        }
    }

    private static int EvaluatePostfixUnary(
        PostfixUnaryExpressionSyntax postfix,
        Dictionary<string, int> variables)
    {
        if (postfix.Operand is not IdentifierNameSyntax identifier)
        {
            throw new NotSupportedException("自增自减暂只支持局部变量。");
        }

        var name = identifier.Identifier.Text;
        var oldValue = GetVariable(variables, name);
        variables[name] = postfix.IsKind(SyntaxKind.PostIncrementExpression)
            ? unchecked(oldValue + 1)
            : unchecked(oldValue - 1);
        return oldValue;
    }

    private static int EvaluateBinary(BinaryExpressionSyntax binary, Dictionary<string, int> variables)
    {
        unchecked
        {
            if (IsComparison(binary))
            {
                return EvaluateComparison(binary, variables) ? 1 : 0;
            }

            var left = EvaluateInt(binary.Left, variables);
            var right = EvaluateInt(binary.Right, variables);

            return binary.Kind() switch
            {
                SyntaxKind.AddExpression => left + right,
                SyntaxKind.SubtractExpression => left - right,
                SyntaxKind.MultiplyExpression => left * right,
                SyntaxKind.DivideExpression => left / right,
                SyntaxKind.ModuloExpression => left % right,
                SyntaxKind.BitwiseAndExpression => left & right,
                SyntaxKind.BitwiseOrExpression => left | right,
                SyntaxKind.ExclusiveOrExpression => left ^ right,
                SyntaxKind.LeftShiftExpression => left << right,
                SyntaxKind.RightShiftExpression => left >> right,
                _ => throw new NotSupportedException($"AOT 解释器暂不支持二元表达式：{binary.Kind()}。")
            };
        }
    }

    private static bool EvaluateComparison(BinaryExpressionSyntax binary, Dictionary<string, int> variables)
    {
        var left = EvaluateInt(binary.Left, variables);
        var right = EvaluateInt(binary.Right, variables);

        return binary.Kind() switch
        {
            SyntaxKind.EqualsExpression => left == right,
            SyntaxKind.NotEqualsExpression => left != right,
            SyntaxKind.LessThanExpression => left < right,
            SyntaxKind.LessThanOrEqualExpression => left <= right,
            SyntaxKind.GreaterThanExpression => left > right,
            SyntaxKind.GreaterThanOrEqualExpression => left >= right,
            _ => throw new NotSupportedException($"AOT 解释器暂不支持条件表达式：{binary.Kind()}。")
        };
    }

    private static int EvaluateAssignment(
        AssignmentExpressionSyntax assignment,
        Dictionary<string, int> variables)
    {
        if (assignment.Left is not IdentifierNameSyntax identifier)
        {
            throw new NotSupportedException("赋值左侧暂只支持局部变量。");
        }

        unchecked
        {
            var name = identifier.Identifier.Text;
            var oldValue = assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                ? 0
                : GetVariable(variables, name);
            var right = EvaluateInt(assignment.Right, variables);
            var value = assignment.Kind() switch
            {
                SyntaxKind.SimpleAssignmentExpression => right,
                SyntaxKind.AddAssignmentExpression => oldValue + right,
                SyntaxKind.SubtractAssignmentExpression => oldValue - right,
                SyntaxKind.MultiplyAssignmentExpression => oldValue * right,
                SyntaxKind.DivideAssignmentExpression => oldValue / right,
                SyntaxKind.ModuloAssignmentExpression => oldValue % right,
                SyntaxKind.AndAssignmentExpression => oldValue & right,
                SyntaxKind.OrAssignmentExpression => oldValue | right,
                SyntaxKind.ExclusiveOrAssignmentExpression => oldValue ^ right,
                SyntaxKind.LeftShiftAssignmentExpression => oldValue << right,
                SyntaxKind.RightShiftAssignmentExpression => oldValue >> right,
                _ => throw new NotSupportedException($"AOT 解释器暂不支持赋值表达式：{assignment.Kind()}。")
            };

            variables[name] = value;
            return value;
        }
    }

    private static int EvaluateInvocation(
        InvocationExpressionSyntax invocation,
        Dictionary<string, int> variables)
    {
        var methodName = invocation.Expression.ToString();
        var arguments = invocation.ArgumentList.Arguments
            .Select(argument => EvaluateInt(argument.Expression, variables))
            .ToArray();

        return methodName switch
        {
            "Math.Abs" or "System.Math.Abs" when arguments.Length == 1 => Math.Abs(arguments[0]),
            "Math.Min" or "System.Math.Min" when arguments.Length == 2 => Math.Min(arguments[0], arguments[1]),
            "Math.Max" or "System.Math.Max" when arguments.Length == 2 => Math.Max(arguments[0], arguments[1]),
            _ => throw new NotSupportedException($"AOT 解释器暂不支持调用：{methodName}。")
        };
    }

    private static int IncrementVariable(
        ExpressionSyntax expression,
        Dictionary<string, int> variables,
        int delta)
    {
        if (expression is not IdentifierNameSyntax identifier)
        {
            throw new NotSupportedException("自增自减暂只支持局部变量。");
        }

        var name = identifier.Identifier.Text;
        var value = unchecked(GetVariable(variables, name) + delta);
        variables[name] = value;
        return value;
    }

    private static int GetVariable(Dictionary<string, int> variables, string name)
    {
        return variables.TryGetValue(name, out var value)
            ? value
            : throw new InvalidOperationException($"变量未定义：{name}。");
    }

    private static bool IsCalMethod(MethodDeclarationSyntax method)
    {
        if (method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault() is null)
        {
            return false;
        }

        if (!string.Equals(method.Identifier.Text, "Cal", StringComparison.Ordinal))
        {
            return false;
        }

        if (!IsIntType(method.ReturnType))
        {
            return false;
        }

        var parameters = method.ParameterList.Parameters;
        return parameters.Count == 2 &&
               IsIntType(parameters[0].Type) &&
               IsIntType(parameters[1].Type);
    }

    private static bool IsIntType(TypeSyntax? type)
    {
        return type?.ToString() is "int" or "Int32" or "System.Int32";
    }

    private static bool IsComparison(BinaryExpressionSyntax binary)
    {
        return binary.Kind() is SyntaxKind.EqualsExpression
            or SyntaxKind.NotEqualsExpression
            or SyntaxKind.LessThanExpression
            or SyntaxKind.LessThanOrEqualExpression
            or SyntaxKind.GreaterThanExpression
            or SyntaxKind.GreaterThanOrEqualExpression;
    }

    private static string GetTypeName(MethodDeclarationSyntax method)
    {
        var classNames = method.Ancestors()
            .OfType<ClassDeclarationSyntax>()
            .Reverse()
            .Select(x => x.Identifier.Text);
        var namespaceNames = method.Ancestors()
            .Where(x => x is BaseNamespaceDeclarationSyntax)
            .Cast<BaseNamespaceDeclarationSyntax>()
            .Reverse()
            .Select(x => x.Name.ToString());
        var parts = namespaceNames.Concat(classNames).ToArray();
        return parts.Length == 0 ? "<unknown>" : string.Join(".", parts);
    }

    private readonly record struct ExecutionFlow(bool HasReturned, int ReturnValue)
    {
        public static ExecutionFlow None { get; } = new(false, 0);

        public static ExecutionFlow Return(int value)
        {
            return new ExecutionFlow(true, value);
        }
    }
}
