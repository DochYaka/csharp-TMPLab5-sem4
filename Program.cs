using System.Text;

namespace lab5;

internal static class Program
{
    private sealed record OperatorInfo(int Priority, bool RightAssociative, int Arity);

    private static readonly Dictionary<string, OperatorInfo> Operators = new()
    {
        ["+"] = new OperatorInfo(1, false, 2),
        ["-"] = new OperatorInfo(1, false, 2),
        ["*"] = new OperatorInfo(2, false, 2),
        ["/"] = new OperatorInfo(2, false, 2),
        [":"] = new OperatorInfo(2, false, 2),
        ["^"] = new OperatorInfo(3, true, 2),
        ["**"] = new OperatorInfo(3, true, 2),
        ["u+"] = new OperatorInfo(4, true, 1),
        ["u-"] = new OperatorInfo(4, true, 1)
    };

    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        var expression = GetExpressionFromArgsOrInput();

        if (string.IsNullOrWhiteSpace(expression))
        {
            Console.WriteLine("Пустое выражение.");
            return;
        }

        try
        {
            var poliz = ConvertToPoliz(expression);
            var result = EvaluatePoliz(poliz);

            Console.WriteLine($"ПОЛИЗ: {poliz}");
            Console.WriteLine($"Результат: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    private static string? GetExpressionFromArgsOrInput()
    {
        if (Environment.GetCommandLineArgs().Length > 1)
        {
            return string.Join(" ", Environment.GetCommandLineArgs().Skip(1));
        }

        Console.WriteLine("Введите выражение (например: 1 - 2 * (3 + 4) ^ 2):");
        return Console.ReadLine();
    }

    /// <summary>
    /// Перевод выражения в ПОЛИЗ.
    /// </summary>
    private static string ConvertToPoliz(string expression)
    {
        var output = new StringBuilder();
        var operators = new Stack<string>();
        var number = new StringBuilder();
        var expectOperand = true;

        void FlushNumber()
        {
            if (number.Length > 0)
            {
                output.Append(number).Append(' ');
                number.Clear();
                expectOperand = false;
            }
        }

        for (var i = 0; i < expression.Length; i++)
        {
            var ch = expression[i];

            if (char.IsWhiteSpace(ch))
            {
                FlushNumber();
                continue;
            }

            if (char.IsDigit(ch))
            {
                number.Append(ch);
                continue;
            }

            FlushNumber();

            string token;
            if (ch == '*' && i + 1 < expression.Length && expression[i + 1] == '*')
            {
                token = "**";
                i++;
            }
            else
            {
                token = ch.ToString();
            }

            if (token == "(")
            {
                operators.Push(token);
                expectOperand = true;
                continue;
            }

            if (token == ")")
            {
                while (operators.Count > 0 && operators.Peek() != "(")
                {
                    output.Append(operators.Pop()).Append(' ');
                }

                if (operators.Count == 0 || operators.Peek() != "(")
                {
                    throw new ArgumentException("Несогласованные скобки.");
                }

                operators.Pop();
                expectOperand = false;
                continue;
            }

            if (token is "+" or "-" && expectOperand)
            {
                token = token == "+" ? "u+" : "u-";
            }

            if (!Operators.ContainsKey(token))
            {
                throw new ArgumentException($"Недопустимый символ: {ch}");
            }

            var current = Operators[token];

            while (operators.Count > 0 &&
                   operators.Peek() != "(" &&
                   Operators.ContainsKey(operators.Peek()) &&
                   ShouldPop(Operators[operators.Peek()], current))
            {
                output.Append(operators.Pop()).Append(' ');
            }

            operators.Push(token);
            expectOperand = true;
        }

        FlushNumber();

        while (operators.Count > 0)
        {
            var op = operators.Pop();
            if (op == "(")
            {
                throw new ArgumentException("Несогласованные скобки.");
            }

            output.Append(op).Append(' ');
        }

        return output.ToString().Trim();
    }

    private static bool ShouldPop(OperatorInfo stackOp, OperatorInfo currentOp)
    {
        if (stackOp.Priority > currentOp.Priority)
        {
            return true;
        }

        return stackOp.Priority == currentOp.Priority && !currentOp.RightAssociative;
    }

    private static int EvaluatePoliz(string poliz)
    {
        var stack = new Stack<int>();
        var tokens = poliz.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            if (int.TryParse(token, out var value))
            {
                stack.Push(value);
                continue;
            }

            if (!Operators.TryGetValue(token, out var opInfo))
            {
                throw new ArgumentException($"Неизвестный оператор: {token}");
            }

            if (stack.Count < opInfo.Arity)
            {
                throw new ArgumentException("Некорректная ПОЛИЗ-запись.");
            }

            if (opInfo.Arity == 1)
            {
                var unaryValue = stack.Pop();
                var unaryResult = token switch
                {
                    "u+" => unaryValue,
                    "u-" => -unaryValue,
                    _ => throw new ArgumentException($"Неизвестный унарный оператор: {token}")
                };

                stack.Push(unaryResult);
                continue;
            }

            var b = stack.Pop();
            var a = stack.Pop();
            var res = token switch
            {
                "+" => a + b,
                "-" => a - b,
                "*" => a * b,
                "/" when b != 0 => a / b,
                ":" when b != 0 => a / b,
                "/" or ":" => throw new DivideByZeroException("Деление на ноль."),
                "^" => PowInt(a, b),
                "**" => PowInt(a, b),
                _ => throw new ArgumentException($"Неизвестный бинарный оператор: {token}")
            };

            stack.Push(res);
        }

        if (stack.Count != 1)
        {
            throw new ArgumentException("Некорректная ПОЛИЗ-запись.");
        }

        return stack.Pop();
    }

    private static int PowInt(int @base, int exponent)
    {
        if (exponent < 0)
        {
            throw new ArgumentException("Для целочисленного режима показатель степени должен быть >= 0.");
        }

        var result = 1;
        var b = @base;
        var e = exponent;

        while (e > 0)
        {
            if ((e & 1) == 1)
            {
                result *= b;
            }

            b *= b;
            e >>= 1;
        }

        return result;
    }
}
