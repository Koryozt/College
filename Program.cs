using College;

// Ejercicio: 49.2324/(x-0.1261050) - 27.045/x^2 - 2.5

Console.Write("Type > f(x) = ");
string? function = Console.ReadLine();

Console.Write("X1? ");
double x1 = double.Parse(Console.ReadLine()!);

Console.Write("X2? ");
double x2 = double.Parse(Console.ReadLine()!);

Console.Write("Tolerance (TOL)? ");
string? tol = Console.ReadLine();

//Console.Write("Steps? ");
//double st = double.Parse(Console.ReadLine());


var methods = new NumericalMethods(7, tol, function);

Console.WriteLine("Biseccion");
_ = methods.BisectionMethod(x1, x2, null);

Console.WriteLine("\nFalsa posicion");
_ = methods.FalsePositionMethod(x1, x2);

double xNR = 1.5 * 0.082054 * 400 / 2.5;

Console.WriteLine("\nNewton Raphson");
_ = methods.NewtonRaphsonMethod(xNR);

Console.WriteLine("\nSecante");
_ = methods.SecantMethod(x1, x2);