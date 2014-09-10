using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpressionCalc
{

    class Program
    {
        static void Main(string[] args)
        {

            List<int> l = new List<int>();
            
            Context ctx = Parser.ParseFile("1.txt");

            while (true)
            {
                try
                {
                    string str = Console.ReadLine();
                    if (str.Contains('='))
                    {
                        string[] split = str.Split('=');
                        ctx.AddConstant(split[0], double.Parse(split[1]));
                    }
                    else if (str.Contains(","))
                    {
                        string[] split = str.Split(',');
                        Expression e = ctx.GetExpression(split[0]);
                        if (e != null)
                        {
                            Expression d = ctx.GetDerivation(e, split[1]);
                            Console.WriteLine(d.ToString());
                            Console.WriteLine(ctx.Calculate(d));
                        }
                    }
                    else if (str == "reset")
                    {
                        ctx = Parser.ParseFile("1.txt");
                        continue;
                    }
                    else if (str == "dump")
                    {
                        Console.Write(ctx.Dump());
                    }
                    else if (str.StartsWith("simplify"))
                    {
                        Expression expr = Parser.ParseExpression(str.Split(' ')[1]);
                        Console.WriteLine(ctx.Simplify(expr).ToString());
                    }
                    else if (str.StartsWith("disc"))
                    {
                        string[] split = str.Split(' ');
                        Console.WriteLine(DiscRotation.Generate(Double.Parse(split[1])).ToString());
                    }
                    else
                    {
                        Expression expr = Parser.ParseExpression(str);
                        Console.WriteLine(ctx.Calculate(expr));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            Console.ReadKey();
        }
    }
}
