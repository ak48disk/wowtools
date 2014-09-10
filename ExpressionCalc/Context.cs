using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionCalc
{
    public class Context
        : IArgumentProvider
    {
        public Context()
        {
        }

        public void ClearConstants()
        {
            predefinedConstants = new Dictionary<string, double>();
        }

        private bool ExistsConstantOrExpression(string name)
        {
            return predefinedConstants.ContainsKey(name) || definedExpressions.ContainsKey(name);
        }

        public void AddExpression(string name, Expression expr)
        {
            if (ExistsConstantOrExpression(name))
                throw new Exception();
            definedExpressions.Add(name, expr);
        }

        public void AddConstant(string name, double num)
        {
            if (ExistsConstantOrExpression(name))
                throw new Exception();
            predefinedConstants[name] = num;
        }

        public void ChangeConstant(string name, double num)
        {
            if (!predefinedConstants.ContainsKey(name))
                throw new Exception();
            else
            {
                derivedNumbers.Clear();
                predefinedConstants[name] = num;
            }
        }

        public Expression GetExpression(string name)
        {
            if (!definedExpressions.ContainsKey(name))
                throw new Exception(name + " not defined");
            return definedExpressions[name];
        }

        public double Calculate(Expression expr)
        {
            if (!expr.Calculatable(this))
            {
                var preReqs = expr.GetDependentParamerters().Where(_ => !ContainArgument(_)).Distinct().ToDictionary(_ => _, GetExpression);
                //while (preReqs.Count != 0)
                //{
                    /*var calculable = preReqs.Where(_ => _.Value.Calculatable(this)).ToList();
                    /*if (calculable.Count == 0)
                        throw new Exception(string.Format("No calculable from elements {0}",string.Join(" ",preReqs.Select(_ => _.Key))));
                    if (predefinedConstants.ContainsKey(calculable.First().Key))
                    predefinedConstants.Add(calculable.First().Key, calculable.First().Value.Calculate(this));*/
                //}
                foreach (var kvp in preReqs)
                {
                    if (!ContainArgument(kvp.Key))
                        derivedNumbers.Add(kvp.Key, Calculate(kvp.Value));
                }
            }
            return expr.Calculate(this);
        }

        public Expression Simplify(Expression expr)
        {
            bool complete = false;
            while (!complete)
            {
                complete = true;
                foreach (var name in expr.GetDependentParamerters())
                {
                    if (ExistsConstantOrExpression(name))
                    {
                        if (ContainArgument(name))
                            expr = expr.ReplaceParameter(name,new ConstantExpression(GetArgument(name)));
                        else 
                            expr = expr.ReplaceParameter(name,GetExpression(name));
                        complete = false;
                    }
                }
            }
            return expr.Normalize();
        }

        private Expression GetExpressionReplaced(Expression expr, string name)
        {
            bool changed = false;
            foreach (var str in expr.GetDependentParamerters())
            {
                if (definedExpressions.ContainsKey(str))
                {
                    var exp = definedExpressions[str];
                    var replaced = GetExpressionReplaced(exp,name);
                    if (replaced != null)
                    {
                        expr = expr.ReplaceParameter(str, replaced);
                        changed = true;
                    }
                }
            }
            if (changed || expr.GetDependentParamerters().Contains(name) )
                return expr;
            else
                return null;
        }

        public Expression GetDerivation(Expression expr, string name)
        {
            var exp = GetExpressionReplaced(expr, name);
            if (exp != null)
                expr = exp;
            return expr.GetDerivation(name).Normalize();
        }
        

        private Dictionary<string, double> predefinedConstants = new Dictionary<string,double>();
        private Dictionary<string, double> derivedNumbers = new Dictionary<string, double>();
        private Dictionary<string, Expression> definedExpressions = new Dictionary<string, Expression>();


        public bool ContainArgument(string argumentName)
        {
            return predefinedConstants.ContainsKey(argumentName) || derivedNumbers.ContainsKey(argumentName);
        }

        public double GetArgument(string argumentName)
        {
            if (predefinedConstants.ContainsKey(argumentName))
                return predefinedConstants[argumentName];
            return derivedNumbers[argumentName];
        }

        public string Dump()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var expression in definedExpressions)
            {
                sb.AppendLine(string.Format("define {0} = {1};",expression.Key,expression.Value.ToString()));
            }
            foreach(var constant in predefinedConstants)
            {
                sb.AppendLine(string.Format("let {0} = {1};",constant.Key,constant.Value.ToString()));
            }
            return sb.ToString();
        }
    }
}
