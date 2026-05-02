/*
NOD SYSTEM -- EXPRESSION EVALUATOR

Dit bestand voert NOD 2.0 math-expressies uit.

Voorbeelden:
- ans^2
- e^2
- ans * e^2
- ans e^2
- pi * ans^2
- ln(e)
- log(ans,2)
- sqrt((ans^2 + 25) / 3)
- 5!
- comb(5,2)
- |ans|

Belangrijk:
- e en pi worden hier als constanten herkend.
- ans is de actuele invoer-/tussenwaarde.
- variabelen uit EquationEngine kunnen ook worden meegegeven.
*/

using System.Globalization;

namespace NodSystem.Core;

// Zoek/commentaar: Type-overzicht: class NodExpressionEvaluator bevat de hoofdlogica/data voor dit onderdeel.
public static class NodExpressionEvaluator
{
    // Zoek/commentaar: Evalueert een expressie of berekening voor Evaluate.
    public static decimal Evaluate(string expression, decimal ans)
    {
        return Evaluate(expression, ans, new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase));
    }

    // Zoek/commentaar: Evalueert een expressie of berekening voor Evaluate.
    public static decimal Evaluate(string expression, decimal ans, IReadOnlyDictionary<string, decimal> variables)
    {
        var parser = new Parser(expression, (double)ans, variables);
        var value = parser.ParseExpression();
        parser.ExpectEnd();
        return (decimal)value;
    }

    // Zoek/commentaar: Type-overzicht: class Parser bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class Parser
    {
        private static readonly double ConstE = Math.E;
        private static readonly double ConstPi = Math.PI;

        private readonly string _text;
        private readonly double _ans;
        private readonly IReadOnlyDictionary<string, decimal> _variables;
        private int _position;
        private bool _commasSeparateArguments;

        // Zoek/commentaar: Constructor: maakt en initialiseert Parser.
        public Parser(string text, double ans, IReadOnlyDictionary<string, decimal> variables)
        {
            _text = text;
            _ans = ans;
            _variables = variables;
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseExpression.
        public double ParseExpression()
        {
            var value = ParseTerm();
            while (true)
            {
                if (Match('+')) value += ParseTerm();
                else if (Match('-')) value -= ParseTerm();
                else return value;
            }
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseTerm.
        private double ParseTerm()
        {
            var value = ParsePower();
            while (true)
            {
                if (Match('*')) value *= ParsePower();
                else if (Match('/')) value /= ParsePower();
                else if (Match('%')) value %= ParsePower();
                else if (StartsImplicitMultiplication()) value *= ParsePower();
                else return value;
            }
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParsePower.
        private double ParsePower()
        {
            var value = ParseUnary();
            if (Match('^')) value = Math.Pow(value, ParsePower());
            return value;
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseUnary.
        private double ParseUnary()
        {
            if (Match('+')) return ParseUnary();
            if (Match('-')) return -ParseUnary();
            return ParsePostfix();
        }

        private double ParsePostfix()
        {
            var value = ParsePrimary();
            while (Match('!'))
                value = Factorial(value);
            return value;
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParsePrimary.
        private double ParsePrimary()
        {
            if (Match('('))
            {
                var value = ParseExpression();
                Expect(')');
                return value;
            }

            if (Match('|'))
            {
                var value = Math.Abs(ParseExpression());
                Expect('|');
                return value;
            }

            if (char.IsDigit(Current) || Current is ',' or '.') return ParseNumber();
            if (char.IsLetter(Current) || Current == '\u03C0') return ParseIdentifierOrFunction();

            throw Error($"Unexpected character '{Current}'");
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseNumber.
        private double ParseNumber()
        {
            var start = _position;
            while (char.IsDigit(Current) || Current == '.' || (!_commasSeparateArguments && Current == ',')) _position++;
            var raw = _text[start.._position].Replace(',', '.');

            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                return number;

            throw Error($"Invalid number '{raw}'");
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseIdentifierOrFunction.
        private double ParseIdentifierOrFunction()
        {
            var name = ParseIdentifier();

            if (Match('('))
            {
                var args = new List<double>();
                var previousCommaMode = _commasSeparateArguments;
                _commasSeparateArguments = true;

                try
                {
                    if (!Peek(')'))
                    {
                        while (true)
                        {
                            args.Add(ParseExpression());
                            if (!Match(',')) break;
                        }
                    }

                    Expect(')');
                }
                finally
                {
                    _commasSeparateArguments = previousCommaMode;
                }

                return CallFunction(name.ToLowerInvariant(), args);
            }

            if (_variables.TryGetValue(name, out var value))
                return (double)value;

            return name.ToLowerInvariant() switch
            {
                "ans" or "x" => _ans,
                "e" => ConstE,
                "pi" or "\u03C0" => ConstPi,
                _ => throw Error($"Unknown variable '{name}'")
            };
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseIdentifier.
        private string ParseIdentifier()
        {
            var start = _position;
            while (char.IsLetterOrDigit(Current) || Current == '_' || Current == '\u03C0') _position++;
            return _text[start.._position];
        }

        // Zoek/commentaar: Methode CallFunction: centrale logica voor deze stap.
        private static double CallFunction(string name, IReadOnlyList<double> args) => name switch
        {
            "abs" when args.Count == 1 => Math.Abs(args[0]),
            "sqrt" or "sqr" when args.Count == 1 => Math.Sqrt(args[0]),
            "pow" when args.Count == 2 => Math.Pow(args[0], args[1]),
            "exp" when args.Count == 1 => Math.Exp(args[0]),
            "exp" when args.Count == 2 => Math.Pow(args[0], args[1]),
            "ln" when args.Count == 1 => Math.Log(args[0]),
            "log" when args.Count == 2 => Math.Log(args[0], args[1]),
            "log" when args.Count == 1 => Math.Log10(args[0]),
            "sin" when args.Count == 1 => Math.Sin(args[0]),
            "cos" when args.Count == 1 => Math.Cos(args[0]),
            "tan" when args.Count == 1 => Math.Tan(args[0]),

            // Inverse trigonometrie in radialen.
            "asin" when args.Count == 1 => Math.Asin(args[0]),
            "acos" when args.Count == 1 => Math.Acos(args[0]),
            "atan" when args.Count == 1 => Math.Atan(args[0]),

            // Graden <-> radialen.
            "rad" when args.Count == 1 => args[0] * Math.PI / 180.0,
            "deg" when args.Count == 1 => args[0] * 180.0 / Math.PI,

            // Trigonometrie in graden.
            "sind" when args.Count == 1 => Math.Sin(args[0] * Math.PI / 180.0),
            "cosd" when args.Count == 1 => Math.Cos(args[0] * Math.PI / 180.0),
            "tand" when args.Count == 1 => Math.Tan(args[0] * Math.PI / 180.0),

            // Inverse trigonometrie met uitkomst in graden.
            "asind" when args.Count == 1 => Math.Asin(args[0]) * 180.0 / Math.PI,
            "acosd" when args.Count == 1 => Math.Acos(args[0]) * 180.0 / Math.PI,
            "atand" when args.Count == 1 => Math.Atan(args[0]) * 180.0 / Math.PI,
            "mod" when args.Count == 2 => args[0] % args[1],
            "rem" when args.Count == 2 => args[0] % args[1],
            "fact" or "factorial" when args.Count == 1 => Factorial(args[0]),
            "comb" or "ncr" or "choose" when args.Count == 2 => Combination(args[0], args[1]),
            "perm" or "npr" when args.Count == 2 => Permutation(args[0], args[1]),
            "expected" or "expect" when args.Count >= 2 && args.Count % 2 == 0 => ExpectedValue(args),
            "min" when args.Count == 2 => Math.Min(args[0], args[1]),
            "max" when args.Count == 2 => Math.Max(args[0], args[1]),
            "round" when args.Count == 1 => Math.Round(args[0]),
            "floor" when args.Count == 1 => Math.Floor(args[0]),
            "ceil" when args.Count == 1 => Math.Ceiling(args[0]),
            _ => throw new FormatException($"Unknown function or wrong argument count: {name}")
        };

        private static double Factorial(double value)
        {
            var n = RequireWholeNumber(value, "factorial");
            if (n < 0)
                throw new FormatException("factorial expects a non-negative whole number.");
            if (n > 27)
                throw new FormatException("factorial result is too large for NOD decimal output.");

            double result = 1;
            for (var i = 2; i <= n; i++)
                result *= i;
            return result;
        }

        private static double Combination(double nValue, double rValue)
        {
            var n = RequireWholeNumber(nValue, "comb");
            var r = RequireWholeNumber(rValue, "comb");
            if (n < 0 || r < 0 || r > n)
                throw new FormatException("comb expects whole numbers with 0 <= r <= n.");

            r = Math.Min(r, n - r);
            double result = 1;
            for (var i = 1; i <= r; i++)
                result = result * (n - r + i) / i;
            return result;
        }

        private static double Permutation(double nValue, double rValue)
        {
            var n = RequireWholeNumber(nValue, "perm");
            var r = RequireWholeNumber(rValue, "perm");
            if (n < 0 || r < 0 || r > n)
                throw new FormatException("perm expects whole numbers with 0 <= r <= n.");

            double result = 1;
            for (var i = 0; i < r; i++)
                result *= n - i;
            return result;
        }

        private static double ExpectedValue(IReadOnlyList<double> args)
        {
            double result = 0;
            for (var i = 0; i < args.Count; i += 2)
                result += args[i] * args[i + 1];
            return result;
        }

        private static int RequireWholeNumber(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value - Math.Round(value)) > 0.0000000001)
                throw new FormatException($"{name} expects whole numbers.");
            return (int)Math.Round(value);
        }

        // Zoek/commentaar: Methode StartsImplicitMultiplication: centrale logica voor deze stap.
        private bool StartsImplicitMultiplication()
        {
            SkipWhite();
            return Current == '(' || char.IsLetter(Current) || Current == '\u03C0';
        }

        // Zoek/commentaar: Methode ExpectEnd: centrale logica voor deze stap.
        public void ExpectEnd()
        {
            SkipWhite();
            if (_position < _text.Length) throw Error($"Unexpected text '{_text[_position..]}'");
        }

        // Zoek/commentaar: Methode Match: centrale logica voor deze stap.
        private bool Match(char ch)
        {
            SkipWhite();
            if (Current != ch) return false;
            _position++;
            return true;
        }

        // Zoek/commentaar: Methode Peek: centrale logica voor deze stap.
        private bool Peek(char ch)
        {
            SkipWhite();
            return Current == ch;
        }

        // Zoek/commentaar: Methode Expect: centrale logica voor deze stap.
        private void Expect(char ch)
        {
            if (!Match(ch)) throw Error($"Expected '{ch}'");
        }

        // Zoek/commentaar: Methode SkipWhite: centrale logica voor deze stap.
        private void SkipWhite()
        {
            while (char.IsWhiteSpace(Current)) _position++;
        }

        private char Current => _position < _text.Length ? _text[_position] : '\0';

        // Zoek/commentaar: Methode Error: centrale logica voor deze stap.
        private FormatException Error(string message) =>
            new($"{message} at position {_position} in expression '{_text}'.");
    }
}
