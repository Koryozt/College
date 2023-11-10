using College;

// Ejercicio: 2.5*pi*x^2-5*x^2*asin(2/x)-10*(x^2-4)^0.5-12.4

Console.Write("Type > f(x) = ");
string? function = Console.ReadLine();

//Console.Write("X1? ");
//double x1 = double.Parse(Console.ReadLine()!);

//Console.Write("X2? ");
//double x2 = double.Parse(Console.ReadLine()!);

Console.Write("Tolerance (TOL)? ");
string? tol = Console.ReadLine();

//Console.Write("Steps? ");
//double st = double.Parse(Console.ReadLine());


var methods = new NumericalMethods(7, tol, function);

methods.SecantMethod(2, 3);