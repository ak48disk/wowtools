using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionCalc
{
    class Program
    {
        static void Main(string[] args)
        {


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
