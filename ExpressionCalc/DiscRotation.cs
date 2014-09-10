using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionCalc
{
    class DiscRotation
    {
        public static Expression Generate(double timeLimit)
        {
            Expression penance = Parser.ParseExpression("(1080+1.12*SP)*3").Normalize();
            Expression holyFire = Parser.ParseExpression("1535+1.328*SP").Normalize();
            Expression smite = Parser.ParseExpression("2361+0.856*SP").Normalize();
            double penanceReady = 0;
            double holyFireReady = 0;
            double stackReset = 0;
            double time = 0;
            int stacks = 0;
            Expression[] stacksExpr = new Expression[6] {
                new ConstantExpression(1),
                new ConstantExpression(1.04),
                new ConstantExpression(1.08),
                new ConstantExpression(1.12),
                new ConstantExpression(1.16),
                new ConstantExpression(1.2),
            };
            List<Expression> addExpressions = new List<Expression>();
            while (time < timeLimit)
            {
                if (stackReset <= time)
                {
                    stackReset = time + 30;
                    stacks = 0;
                }
                if (penanceReady <= time)
                {
                    addExpressions.Add(new MultiplyExpression(new List<Expression> { stacksExpr[stacks], penance }));
                    penanceReady = time + 9;
                    time += 2;
                    stacks++;
                }
                else if (holyFireReady <= time)
                {
                    addExpressions.Add(new MultiplyExpression(new List<Expression> { stacksExpr[stacks], holyFire }));
                    holyFireReady = time + 10;
                    time += 1.5;
                    stacks++;
                }
                else
                {
                    addExpressions.Add(new MultiplyExpression(new List<Expression> { stacksExpr[stacks], smite }));
                    time += 1.5;
                    stacks++;
                }
                if (stacks > 5) stacks = 5;
            }
            return new MultiplyExpression(new List<Expression> { 
                new AddExpression(addExpressions).Normalize(), new ConstantExpression(1.0 / time * 1.05 * 0.9) }).Normalize();
        }
    }
}
