using College;

// Ejercicio: (1e6-0.25*1e6*sqrt(c))*1/1e5

Console.Write("Type > variable (Ex. x) = ");
string? variable = Console.ReadLine();

Console.Write($"Type > f({variable}) = ");
string? function = Console.ReadLine();

Console.Write("Type > X1 = ");
double x1 = double.Parse(Console.ReadLine()!);

Console.Write("Type > X2 = ");
double x2 = double.Parse(Console.ReadLine()!);

Console.Write("Type > Tolerance (TOL) = ");
string? tol = Console.ReadLine();

//Console.Write("Steps? ");
//double st = double.Parse(Console.ReadLine());


var methods = new NumericalMethods(6, variable, tol, function);

var r = methods.FixedPointMethod(x1, x2);
Console.WriteLine(r);