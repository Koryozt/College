using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ConsoleTables;
using MathNet.Symbolics;

namespace College
{
    public class NumericalMethods
    {
        public SymbolicExpression Function { get; set; }
        public SymbolicExpression Derivative {  get; set; }
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

        public NumericalMethods(
            int fix, 
            string tolerance, 
            string function)
        {
            Fix = fix;
            Tolerance = ParseTolerance(tolerance);
            HasToUseAproximateError = tolerance.Contains('%');
            Function = SymbolicExpression.Parse(function);
            Derivative = Function.Differentiate(
                SymbolicExpression.Variable("x"));
        }

        private static double ParseTolerance(string tolerance) =>
            tolerance.Contains('%') ? 
                double.Parse(tolerance.Trim('%')) : 
                double.Parse(tolerance);

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

        public double EvaluateFunctionOnPoint(double x)
        {
            try 
            {
                FloatingPoint result = Function.Evaluate(
                    new Dictionary<string, FloatingPoint> 
                    { { "x", x } });

                return Math.Round(result.RealValue, Fix);

            } catch (Exception) 
            {
                Console.WriteLine(x);
            } 

            return double.NaN;
        }
        
        public double EvaluateDerivativeOnPoint(
            double x)
        {
            FloatingPoint value = Derivative.Evaluate(
                new Dictionary<string, FloatingPoint>
                { { "x", x } });

            return Math.Round(value.RealValue, Fix);
        }

        public Dictionary<double, double> EvaluateFunctionOnRange(
            double from,
            double to,
            double steps)
        {
            Dictionary<double, double> valueTable = new();

            for (double i = from;
                i <= to;
                i += steps)
            {
                valueTable[Math.Round(i, Fix)] = EvaluateFunctionOnPoint(i);
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
                Dictionary<double, double> valueTable = EvaluateFunctionOnRange(
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
                double relativeError = Math.Abs((x1 - x2) / 2);

                while (relativeError >= Tolerance || 
                    EvaluateFunctionOnPoint(xr) == 0)
                {
                    iterations++;
                    xr = (x1 + x2) / 2;

                    double fx1 = EvaluateFunctionOnPoint(x1),
                        fx2 = EvaluateFunctionOnPoint(x2),
                        fxr = EvaluateFunctionOnPoint(xr);

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

                    relativeError = Math.Abs((x1 - x2) / 2);

                    string jsonData = JsonConvert.SerializeObject(row);
                    JObject jsonObject = JObject.Parse(jsonData);

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

                    xr = Math.Round((x1 + x2) / 2, Fix);

                    double fx1 = EvaluateFunctionOnPoint(x1),
                    fx2 = EvaluateFunctionOnPoint(x2),
                    fxr = EvaluateFunctionOnPoint(xr);

                    relativeError = previousXr != 0 ?
                        double.Abs((xr - previousXr) / previousXr) * 100 :
                        relativeError;

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

                    string jsonData = JsonConvert.SerializeObject(row);
                    JObject jsonObject = JObject.Parse(jsonData);

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

                    double fx1 = EvaluateFunctionOnPoint(x1),
                        fx2 = EvaluateFunctionOnPoint(x2),
                        xr = 0,
                        fxr = 0;
                    
                    try
                    {
                        xr = x2 - (fx2 * (x2 - x1) /
                        (fx2 - fx1));
                        fxr = EvaluateFunctionOnPoint(xr);
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

                    relativeError = Math.Abs(fxr);

                    string jsonData = JsonConvert.SerializeObject(row);
                    JObject jsonObject = JObject.Parse(jsonData);

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


                    double fx1 = EvaluateFunctionOnPoint(x1),
                        fx2 = EvaluateFunctionOnPoint(x2),
                        xr = 0,
                        fxr = 0;

                    try
                    {
                        xr = x2 - (fx2 * (x2 - x1) /
                        (fx2 - fx1));
                        fxr = EvaluateFunctionOnPoint(xr);
                    }
                    catch (DivideByZeroException)
                    {
                        Console.WriteLine($"{fx2} - {fx1} gives 0.");
                        throw;
                    }

                    relativeError = previousXr != 0 ?
                        Math.Abs((xr - previousXr) / previousXr) * 100 :
                        relativeError;

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

                    string jsonData = JsonConvert.SerializeObject(row);
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

            // Considering that TOL is presented in the format with %.
            // Must update in the future.

            double relativeError = double.PositiveInfinity;
            
            var table = new List<JObject>();

            do
            {
                iterations++;

                double prev_x1 = x1,
                    prev_fx1 = EvaluateFunctionOnPoint(x1),
                    prev_der_fx1 = EvaluateDerivativeOnPoint(x1);

                double xr = Math.Round(x1 - prev_fx1 / prev_der_fx1, Fix),
                    fxr = EvaluateFunctionOnPoint(xr);

                relativeError = Math.Round(Math.Abs(xr - x1) * 100, Fix);
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

                string jsonData = JsonConvert.SerializeObject(row);
                JObject jsonObject = JObject.Parse(jsonData);

                table.Add(jsonObject);
            } while (relativeError >= Tolerance);

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
            double relativeError = Math.Abs(x2-x1) / 2;

            var table = new List<JObject>();

            while (relativeError >= Tolerance)
            {
                iterations++;

                double fx1 = EvaluateFunctionOnPoint(x1),
                    fx2 = EvaluateFunctionOnPoint(x2),
                    xr = 0,
                    fxr = 0;

                try
                {
                    xr = x2 - (fx2 * (x2 - x1) /
                    (fx2 - fx1));
                    fxr = EvaluateFunctionOnPoint(xr);
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

                relativeError = Math.Abs(x2 - x1) / 2;

                x1 = x2;
                x2 = xr;

                string jsonData = JsonConvert.SerializeObject(row);
                JObject jsonObject = JObject.Parse(jsonData);

                table.Add(jsonObject);
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
    }
}