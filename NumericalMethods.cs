using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ConsoleTables;
using MathNet.Symbolics;
using static System.Math;

namespace College
{
    public class NumericalMethods
    {
        public SymbolicExpression Function { get; set; }
        public SymbolicExpression Derivative {  get; set; }
        public string Variable { get; set; }
        public double Tolerance { get; set; }
        public int Fix { get; set; }
        private bool HasToUseAproximateError { get; set; }
            
        private static string[] BisectionMethodColumns
        {
            get =>
                new string[] 
                { 
                    "Iteration", 
                    "X1", 
                    "X2", 
                    "XR", 
                    "F(X1)", 
                    "F(X2)", 
                    "F(XR)", 
                    "Error" 
                };
        }
        private static string[] FalsePositionMethodColumns
        {
            get =>
                new string[]
                {
                    "Iteration",
                    "X1",
                    "X2",
                    "F(X1)",
                    "F(X2)",
                    "XR",
                    "F(XR)",
                    "Error"
                };
        }
        private static string[] NewtonRaphsonMethodColumns
        {
            get =>
                new string[]
                {
                    "Iteration",
                    "X1",
                    "F(X1)",
                    "F'(X1)",
                    "XR",
                    "F(XR)",
                    "Error"
                };
        }
        private static string[] FixedPointMethodColumns
        {
            get => new string[]{
                "Iteration",
                "XO",
                "G(XO)",
                "Error"
            };
        }

        public NumericalMethods(
            int fix, 
            string variable,
            string tolerance, 
            string function)
        {
            Fix = fix;
            Variable = variable;
            Tolerance = ParseTolerance(tolerance);
            HasToUseAproximateError = tolerance.Contains('%');
            Function = SymbolicExpression.Parse(function);
            Derivative = Function.Differentiate(
                SymbolicExpression.Variable(Variable));
        }

        private static double ParseTolerance(string tolerance) =>
            tolerance.Contains('%') ? 
                double.Parse(tolerance.Trim('%')) : 
                double.Parse(tolerance);

        private double GetAproximateError(
            double act,
            double prev) =>
                Round(Abs((act - prev) / act) * 100, Fix);

        private static double[] GetRootInterval(
            Dictionary<double, double> values)
        {
            double[] valuesArray = values.Values.ToArray(),
                foundInterval = Array.Empty<double>();

            for (int i = 0; i < valuesArray.Length; i++)
            {
                try
                {
                    if (valuesArray[i] * valuesArray[i + 1] < 0)
                    {
                        foundInterval = new double[] { 
                            valuesArray[i], 
                            valuesArray[i + 1] };
                        
                        break;
                    }
                }
                catch (IndexOutOfRangeException e)
                {
                    Console.WriteLine(
                        "Sequence does not contain a sign change");

                    throw e;
                }
            }

            if (foundInterval != null && foundInterval.Length == 2) 
            {
                double x1 = values.FirstOrDefault(key => 
                key.Value == foundInterval[0]).Key,
                    
                x2 = values.FirstOrDefault(key => 
                key.Value == foundInterval[1]).Key;

                return new double[] { x1, x2 };
            }

            return foundInterval!;
        }

        private bool CanUseMethod(
            RootMethods method, 
            double a, 
            double b)
        {
            switch(method)
            {
                case RootMethods.FixedPoint:
                    double a_eval = Eval(a),
                            b_eval = Eval(b),
                            a_deval = EvalDerivative(a-0.01),
                            b_deval = EvalDerivative(b-0.01);

                    return a_eval >= a && a_eval <= b &&
                        b_eval >= a && b_eval <= b &&
                        a_deval < 1 && b_deval < 1;

                default:
                    return false;
            };
        }

        public double Eval(double x)
        {
            try 
            {
                FloatingPoint result = Function.Evaluate(
                    new Dictionary<string, FloatingPoint> 
                    { { Variable, x } });

                return Round(result.RealValue, Fix);

            } catch (Exception) 
            {
                Console.WriteLine($"{x} Produces an error on function {Function}");
            } 

            return double.NaN;
        }
        
        public double EvalDerivative(double x)
        {
            try
            {
                FloatingPoint value = Derivative.Evaluate(
               new Dictionary<string, FloatingPoint>
               { { Variable, x } });

                return Round(value.RealValue, Fix);
            } catch(Exception) 
            {
                Console.WriteLine($"{x} Produces an error on function derivate {Derivative}");
            }

            return double.NaN;
        }

        public Dictionary<double, double> EvalOnRange(
            double from,
            double to,
            double steps)
        {
            Dictionary<double, double> valueTable = new();

            for (double i = from;
                i <= to;
                i += steps)
            {
                valueTable[Round(i, Fix)] = Eval(i);
            }

            return valueTable;
        }

        public List<JObject> BisectionMethod(
            double x1,
            double x2,
            double? steps)
        {
            int iterations = 0;

            if (steps is not null)
            {
                Dictionary<double, double> valueTable = 
                    EvalOnRange(
                        x1,
                        x2,
                        (double)steps);

                var interval = GetRootInterval(valueTable);
                x1 = interval[0];
                x2 = interval[1];
            }

            double xr = 0;
            var table = new List<JObject>();

            if (!HasToUseAproximateError)
            {
                double relativeError = Abs((x1 - x2) / 2);

                while (relativeError >= Tolerance || 
                    Eval(xr) != 0)
                {
                    iterations++;
                    xr = Round((x1 + x2) / 2, Fix);

                    double fx1 = Eval(x1),
                        fx2 = Eval(x2),
                        fxr = Eval(xr);

                    var row = new
                    {
                        Iteration = iterations,
                        X1 = x1,
                        X2 = x2,
                        XR = xr,
                        F_X1 = fx1,
                        F_X2 = fx2,
                        F_XR = fxr,
                        Error = relativeError
                    };

                    if (fxr * fx1 < 0)
                        x2 = xr;
                    else
                        x1 = xr;

                    relativeError = Round(
                        Abs((x1 - x2) / 2), 
                        Fix);

                    string jsonData = JsonConvert
                        .SerializeObject(row);
                    JObject jsonObject = JObject
                        .Parse(jsonData);

                    table.Add(jsonObject);
                }
            }
            else
            {
                double previousXr = 0,
                    relativeError = double.PositiveInfinity;

                while (relativeError >= Tolerance)
                {
                    iterations++;

                    xr = Round((x1 + x2) / 2, Fix);

                    double fx1 = Eval(x1),
                    fx2 = Eval(x2),
                    fxr = Eval(xr);

                    relativeError = GetAproximateError(
                        xr, previousXr);

                    var row = new
                    {
                        Iteration = iterations,
                        X1 = x1,
                        X2 = x2,
                        XR = xr,
                        F_X1 = fx1,
                        F_X2 = fx2,
                        F_XR = fxr,
                        Error = relativeError
                    };

                    if (fxr * fx1 < 0)
                        x2 = xr;
                    else
                        x1 = xr;

                    previousXr = xr;

                    string jsonData = JsonConvert
                        .SerializeObject(row);
                    
                    JObject jsonObject = JObject
                        .Parse(jsonData);

                    table.Add(jsonObject);
                }
            }

            var tabulate = new ConsoleTable(BisectionMethodColumns);

            foreach (var value in table)
            {
                tabulate.AddRow(
                    value["Iteration"],
                    value["X1"],
                    value["X2"],
                    value["XR"],
                    value["F_X1"],
                    value["F_X2"],
                    value["F_XR"],
                    value["Error"]);
            }

            tabulate.Write();
            Console.WriteLine();

            return table;
        }
    
        public List<JObject> FalsePositionMethod(
            double x1,
            double x2)
        {
            int iterations = 0;
            var table = new List<JObject>();

            if (!HasToUseAproximateError)
            {
                double relativeError = double.PositiveInfinity;
                
                while (relativeError >= Tolerance)
                {
                    iterations++;

                    double fx1 = Eval(x1),
                        fx2 = Eval(x2),
                        xr = 0,
                        fxr = 0;
                    
                    try
                    {
                        xr = Round(
                            x2 - (fx2 * (x2 - x1) / (fx2 - fx1)), 
                            Fix);
                        fxr = Eval(xr);
                    } 
                    catch(DivideByZeroException)
                    {
                        Console.WriteLine($"{fx2} - {fx1} gives 0.");
                        throw;
                    }

                    var row = new
                    {
                        Iteration = iterations,
                        X1 = x1,
                        X2 = x2,
                        F_X1 = fx1,
                        F_X2 = fx2,
                        XR = xr,
                        F_XR = fxr,
                        Error = relativeError
                    };

                    if (fxr * fx1 < 0)
                        x2 = xr;
                    else
                        x1 = xr;

                    relativeError = Abs(fxr);

                    string jsonData = JsonConvert
                        .SerializeObject(row);
                    
                    JObject jsonObject = JObject
                        .Parse(jsonData);

                    table.Add(jsonObject);
                }

            }
            else
            {
                double relativeError = double.PositiveInfinity, 
                    previousXr = 0;

                while (relativeError >= Tolerance)
                {
                    iterations++;

                    double fx1 = Eval(x1),
                        fx2 = Eval(x2),
                        xr = 0,
                        fxr = 0;

                    try
                    {
                        xr = Math.Round(
                            x2 - (fx2 * (x2 - x1) / (fx2 - fx1)), 
                            Fix);
                        fxr = Eval(xr);
                    }
                    catch (DivideByZeroException)
                    {
                        Console.WriteLine($"{fx2} - {fx1} gives 0.");
                        throw;
                    }

                    relativeError = GetAproximateError(
                        xr, 
                        previousXr);

                    var row = new
                    {
                        Iteration = iterations,
                        X1 = x1,
                        X2 = x2,
                        F_X1 = fx1,
                        F_X2 = fx2,
                        XR = xr,
                        F_XR = fxr,
                        Error = relativeError
                    };

                    if (fxr * fx1 < 0)
                        x2 = xr;
                    else
                        x1 = xr;

                    previousXr = xr;

                    string jsonData = JsonConvert
                        .SerializeObject(row);
                    JObject jsonObject = JObject.Parse(jsonData);

                    table.Add(jsonObject);
                }
            }

 
            var tabulate = new ConsoleTable(FalsePositionMethodColumns);

            foreach (var value in table)
            {
                tabulate.AddRow(
                    value["Iteration"],
                    value["X1"],
                    value["X2"],
                    value["F_X1"],
                    value["F_X2"],
                    value["XR"],
                    value["F_XR"],
                    value["Error"]);
            }

            tabulate.Write();
            Console.WriteLine();

            return table;
        }
    
        public List<JObject> NewtonRaphsonMethod(
            double x1)
        {
            int iterations = 0;
            Console.WriteLine($"f'(x) = {Derivative}");

            double relativeError = double.PositiveInfinity;
            
            var table = new List<JObject>();

            if (!HasToUseAproximateError)
            {
                do
                {
                    iterations++;

                    double prev_x1 = x1,
                        prev_fx1 = Eval(x1),
                        prev_der_fx1 = EvalDerivative(x1);

                    double xr = Round(
                        x1 - prev_fx1 / prev_der_fx1, 
                        Fix),
                        fxr = Eval(xr);

                    relativeError = Round(
                        Abs(xr - x1) * 100, 
                        Fix);
                    
                    x1 = xr;

                    var row = new
                    {
                        Iteration = iterations,
                        X1 = prev_x1,
                        F_X1 = prev_fx1,
                        Derivative_X1 = prev_der_fx1,
                        XR = xr,
                        F_XR = fxr,
                        Error = relativeError
                    };

                    string jsonData = JsonConvert
                        .SerializeObject(row);
                    
                    JObject jsonObject = JObject
                        .Parse(jsonData);

                    table.Add(jsonObject);
                
                } while (relativeError >= Tolerance);
            }
            else
            {
                do
                {
                    iterations++;

                    double prev_x1 = x1,
                        prev_fx1 = Eval(x1),
                        prev_der_fx1 = EvalDerivative(x1);

                    double xr = Round(
                        x1 - prev_fx1 / prev_der_fx1, 
                        Fix),
                        fxr = Eval(xr);

                    relativeError = GetAproximateError(xr, prev_x1);
                    x1 = xr;

                    var row = new
                    {
                        Iteration = iterations,
                        X1 = prev_x1,
                        F_X1 = prev_fx1,
                        Derivative_X1 = prev_der_fx1,
                        XR = xr,
                        F_XR = fxr,
                        Error = relativeError
                    };

                    string jsonData = JsonConvert
                        .SerializeObject(row);
                    
                    JObject jsonObject = JObject
                        .Parse(jsonData);

                    table.Add(jsonObject);

                } while (relativeError >= Tolerance);

            }
            var tabulate = new ConsoleTable(NewtonRaphsonMethodColumns);

            foreach (var value in table)
            {
                tabulate.AddRow(
                    value["Iteration"],
                    value["X1"],
                    value["F_X1"],
                    value["Derivative_X1"],
                    value["XR"],
                    value["F_XR"],
                    value["Error"]);
            }

            tabulate.Write();
            Console.WriteLine();

            return table;
        }

        public List<JObject> SecantMethod(
            double x1,
            double x2)
        {
            int iterations = 0;
            double relativeError = double.PositiveInfinity;

            var table = new List<JObject>();

            if (!HasToUseAproximateError)
            {
                while (relativeError >= Tolerance)
                {
                    iterations++;

                    double fx1 = Eval(x1),
                        fx2 = Eval(x2),
                        xr = 0,
                        fxr = 0;

                    try
                    {
                        xr = Round(x2 - (fx2 * (x2 - x1) /
                        (fx2 - fx1)),
                        Fix);
                        fxr = Eval(xr);
                    }
                    catch (DivideByZeroException)
                    {
                        Console.WriteLine($"{fx2} - {fx1} gives 0.");
                        throw;
                    }

                    var row = new
                    {
                        Iteration = iterations,
                        X1 = x1,
                        X2 = x2,
                        F_X1 = fx1,
                        F_X2 = fx2,
                        XR = xr,
                        F_XR = fxr,
                        Error = relativeError
                    };

                    relativeError = Round(
                        Abs(x2 - x1) / 2, 
                        Fix);

                    x1 = x2;
                    x2 = xr;

                    string jsonData = JsonConvert
                        .SerializeObject(row);
                    
                    JObject jsonObject = JObject
                        .Parse(jsonData);

                    table.Add(jsonObject);
                }
            }
            else
            {
                double previousXr = 0;

                while (relativeError >= Tolerance)
                {
                    iterations++;

                    double fx1 = Eval(x1),
                        fx2 = Eval(x2),
                        xr = 0,
                        fxr = 0;

                    try
                    {
                        xr = Round(x2 - (fx2 * (x2 - x1) /
                        (fx2 - fx1)),
                        Fix);
                        fxr = Eval(xr);
                    }
                    catch (DivideByZeroException)
                    {
                        Console.WriteLine($"{fx2} - {fx1} gives 0.");
                        throw;
                    }

                    var row = new
                    {
                        Iteration = iterations,
                        X1 = x1,
                        X2 = x2,
                        F_X1 = fx1,
                        F_X2 = fx2,
                        XR = xr,
                        F_XR = fxr,
                        Error = relativeError
                    };

                    relativeError = GetAproximateError(xr, previousXr);

                    x1 = x2;
                    x2 = xr;

                    previousXr = xr;

                    string jsonData = JsonConvert
                        .SerializeObject(row);

                    JObject jsonObject = JObject
                        .Parse(jsonData);

                    table.Add(jsonObject);
                }
            }

            var tabulate = new ConsoleTable(FalsePositionMethodColumns);

            foreach (var value in table)
            {
                tabulate.AddRow(
                    value["Iteration"],
                    value["X1"],
                    value["X2"],
                    value["F_X1"],
                    value["F_X2"],
                    value["XR"],
                    value["F_XR"],
                    value["Error"]);
            }

            tabulate.Write();
            Console.WriteLine();

            return table;
        }

        public double FixedPointMethod(
            double x1,
            double? x2 = null)
        {
            int iterations = 0;
            double relativeError = double.PositiveInfinity;

            var table = new List<JObject>();

            if (!CanUseMethod(RootMethods.FixedPoint, x1, (double)x2))
                return double.NaN;

            double xo = Round(
                x2 is null ? x1 : (x1 + (double)x2) / 2,
                Fix);

            do
            {
                iterations++;

                double temp = xo;
                xo = Eval(temp);
                var row = new
                {
                    Iteration = iterations,
                    XO = temp,
                    G_XO = xo,
                    Error = relativeError
                };

                relativeError = temp - xo != 0 ?
                    GetAproximateError(xo, temp) :
                    relativeError;

                string jsonData = JsonConvert
                    .SerializeObject(row);

                JObject jsonObject = JObject
                    .Parse(jsonData);

                table.Add(jsonObject);

            } while (relativeError >= Tolerance);

            var tabulate = new ConsoleTable(FixedPointMethodColumns);

            foreach (var value in table)
            {
                tabulate.AddRow(
                    value["Iteration"],
                    value["XO"],
                    value["G_XO"],
                    value["Error"]);
            }

            tabulate.Write();
            Console.WriteLine();

            return xo;
        }
    }
}