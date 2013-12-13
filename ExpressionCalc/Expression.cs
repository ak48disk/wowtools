using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpressionCalc
{
    public interface IArgumentProvider
    {
        bool ContainArgument(string argumentName);
        double GetArgument(string argumentName);
    }

    public abstract class Expression
    {
        public abstract string[] GetDependentParamerters();
        public abstract double Calculate(IArgumentProvider argumentProvider);
        public abstract bool Calculatable(IArgumentProvider argumentProvider);
        public abstract Expression GetDerivation(string parameter);
        public abstract Expression Normalize();
        public abstract Expression ReplaceParameter(string name, Expression expr);
        public bool Normalized { get; set; }
        public Expression()
        {
            Normalized = false;
        }
    }

    public class ConstantExpression
        : Expression
    {
        public ConstantExpression(double constant)
        {
            this.Constant = constant;
            Normalized = true;
        }

        public override double Calculate(IArgumentProvider argumentProvider)
        {
            return Constant;
        }

        public override bool Calculatable(IArgumentProvider argumentProvider)
        {
            return true;
        }

        public override Expression GetDerivation(string parameter)
        {
            return new ConstantExpression(0);
        }

        public override Expression Normalize()
        {
            return this;
        }

        public override string[] GetDependentParamerters()
        {
            return new string[0];
        }

        public override string ToString()
        {
            return Constant.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantExpression)
                return (obj as ConstantExpression).Constant == Constant;
            return false;
        }

        public override Expression ReplaceParameter(string name, Expression expr)
        {
            return this;
        }

        public double Constant { get; private set; }
    }

    public class ParameterExpression
        : Expression
    {
        public ParameterExpression(string parameterName)
        {
            this.parameterName = parameterName;
            Normalized = true;
        }

        public override bool Calculatable(IArgumentProvider argumentProvider)
        {
            return argumentProvider.ContainArgument(parameterName);
        }

        public override double Calculate(IArgumentProvider argumentProvider)
        {
            return argumentProvider.GetArgument(parameterName);
        }

        public override string[] GetDependentParamerters()
        {
            return new string[] { parameterName };
        }

        public override Expression GetDerivation(string parameter)
        {
            if (parameterName == parameter)
                return new ConstantExpression(1);
            else
                return new ConstantExpression(0);
        }

        public override Expression Normalize()
        {
            return this;
        }

        public override string ToString()
        {
            return parameterName;
        }

        public override bool Equals(object obj)
        {
            return (obj is ParameterExpression && (obj as ParameterExpression).ParameterName == ParameterName);
        }

        public override Expression ReplaceParameter(string name, Expression expr)
        {
            if (parameterName == name)
                return expr;
            else
                return this;
        }

        public string ParameterName { get { return parameterName; } }
        private string parameterName;
    }

    public class AddExpression
        : Expression
    {
        public AddExpression(List<Expression> operands)
        {
            this.operands = operands;
        }

        public override bool Calculatable(IArgumentProvider argumentProvider)
        {
            return operands.All(_ => _.Calculatable(argumentProvider));
        }

        public override double Calculate(IArgumentProvider argumentProvider)
        {
            double sum = 0;
            foreach (var operand in operands)
            {
                sum += operand.Calculate(argumentProvider);
            }
            return sum;
        }

        public override string[] GetDependentParamerters()
        {
            List<string> s = new List<string>();
            foreach (var operand in operands)
            {
                s.AddRange(operand.GetDependentParamerters());
            }
            return s.ToArray();
        }

        public override Expression GetDerivation(string parameter)
        {
            return new AddExpression(operands.Select(_ => _.GetDerivation(parameter)).ToList());
        }

        private Expression GetExpressionParametersMultiplied(Expression expr)
        {
            if (!expr.Normalized)
                throw new Exception();
            if (expr is MultiplyExpression)
            {
                if ((expr as MultiplyExpression).Operands.First() is ConstantExpression)
                {
                    var o = (expr as MultiplyExpression).Operands.Skip(1);
                    if (o.Count() == 1)
                        return o.Single();
                    else
                    {
                        MultiplyExpression m = new MultiplyExpression(o.ToList());
                        m.Normalized = true;
                        return m;
                    }
                }
                return expr;
            }
            if (expr is ParameterExpression)
                return expr;
            throw new Exception();
        }

        private double GetExpressionConstant(Expression expr)
        {
            if (!expr.Normalized)
                throw new Exception();
            if (expr is MultiplyExpression)
            {
                if ((expr as MultiplyExpression).Operands.First() is ConstantExpression)
                    return ((expr as MultiplyExpression).Operands.First() as ConstantExpression).Constant;
                return 1;
            }
            if (expr is ParameterExpression)
                return 1;
            throw new Exception();
        }

        public override Expression Normalize()
        {
            if (Normalized) return this;

            var middle = operands.Select(_ => _.Normalize()).Where(_ => {
                if (_ is ConstantExpression)
                {
                    if ((_ as ConstantExpression).Constant == 0)
                        return false;
                }
                return true;
            });

            double constant = 0;
            var normalized = new List<Expression>();

            foreach (var operand in middle)
            {
                if (operand is ConstantExpression)
                    constant += operand.Calculate(null);
                else if (operand is AddExpression)
                {
                    foreach (var c in (operand as AddExpression).operands.OfType<ConstantExpression>())
                        constant += c.Constant;
                    normalized.AddRange((operand as AddExpression).operands.Where(_ => !(_ is ConstantExpression)));
                }
                else
                    normalized.Add(operand);
            }

            if (constant != 0)
                normalized.Add(new ConstantExpression(constant));

            normalized = normalized.OrderBy(_ => _, CompareExpression.Instance).ToList();

            var normalized2 = new List<Expression>();
            Expression lastExpression = null;
            foreach (var expression in normalized)
            {
                if (lastExpression != null && !(lastExpression is ConstantExpression) && !(expression is ConstantExpression))
                {
                    var operand1 = GetExpressionParametersMultiplied(lastExpression);
                    var operand2 = GetExpressionParametersMultiplied(expression);
                    if (operand1.Equals(operand2))
                    {
                        var constantd = GetExpressionConstant(lastExpression) + GetExpressionConstant(expression);
                        if (constantd == 0)
                            lastExpression = null;
                        else if (constantd == 1)
                            lastExpression = operand1;
                        else
                        {
                            var expressionOperands = new List<Expression>();
                            expressionOperands.Add(new ConstantExpression(constantd));
                            if (operand1 is ParameterExpression)
                                expressionOperands.Add(operand1);
                            else
                                expressionOperands.AddRange((operand1 as MultiplyExpression).Operands);
                            lastExpression = new MultiplyExpression(expressionOperands);
                            lastExpression.Normalized = true;
                        }
                        continue;
                    }
                }
                if (lastExpression != null) 
                    normalized2.Add(lastExpression);
                lastExpression = expression;
            }
            if (lastExpression != null)
                normalized2.Add(lastExpression);

            if (normalized2.Count() == 1)
                return normalized2.Single();
            if (normalized2.Count() == 0)
                return new ConstantExpression(0);
            var exp = new AddExpression(normalized2);
            exp.Normalized = true;
            return exp;
        }

        public override string ToString()
        {
            return string.Join("+", operands.Select(_ => _.ToString()));
        }

        public override bool Equals(object obj)
        {
            if (obj is AddExpression)
            {
                var thisNormalized = Normalize();
                var objNormailzed = (obj as AddExpression).Normalize();
                if (thisNormalized is AddExpression &&
                    objNormailzed is AddExpression)
                {
                    var thisNormalizedAdd = thisNormalized as AddExpression;
                    var objNormaliedAdd = objNormailzed as AddExpression;
                    if (thisNormalizedAdd.operands.Count != objNormaliedAdd.operands.Count)
                        return false;
                    for (int i = 0; i < thisNormalizedAdd.operands.Count; ++i)
                        if (!thisNormalizedAdd.operands[i].Equals(objNormaliedAdd.operands[i]))
                            return false;
                    return true;
                }
                return thisNormalized.Equals(objNormailzed);
            }
            if (operands.Count == 1)
                return operands.Single().Equals(obj);
            return false;
        }

        public override Expression ReplaceParameter(string name, Expression expr)
        {
            return new AddExpression(operands.Select(_ => _.ReplaceParameter(name, expr)).ToList());
        }

        public ReadOnlyCollection<Expression> Operands { get { return operands.AsReadOnly(); } }
        private List<Expression> operands;
    }

    public class MultiplyExpression
        : Expression
    {
        public MultiplyExpression(List<Expression> operands)
        {
            this.operands = operands;
        }

        public override bool Calculatable(IArgumentProvider argumentProvider)
        {
            return operands.All(_ => _.Calculatable(argumentProvider));
        }

        public override double Calculate(IArgumentProvider argumentProvider)
        {
            if (operands.Count == 0) return 0;
            double result = 1;
            foreach (var operand in operands)
            {
                result *= operand.Calculate(argumentProvider);
            }
            return result;
        }

        public override string[] GetDependentParamerters()
        {
            List<string> s = new List<string>();
            foreach (var operand in operands)
            {
                s.AddRange(operand.GetDependentParamerters());
            }
            return s.ToArray();
        }

        public override string ToString()
        {
            return string.Join("*", operands.Select(_ => _ is AddExpression ? "(" + _.ToString() + ")" : _.ToString()));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Expression)) 
                return false;
            var thisNormalized = this.Normalize();
            var objNormalized = (obj as Expression).Normalize();
            if (thisNormalized is MultiplyExpression)
            {
                if (!(objNormalized is MultiplyExpression)) return false;
                var thisNormalizedMultiply = thisNormalized as MultiplyExpression;
                var objNormalizedMultiply = objNormalized as MultiplyExpression;
                if (thisNormalizedMultiply.Operands.Count != objNormalizedMultiply.Operands.Count)
                    return false;
                for (int i = 0; i < thisNormalizedMultiply.Operands.Count; ++i)
                {
                    if (!thisNormalizedMultiply.Operands[i].Equals(objNormalizedMultiply.Operands[i]))
                        return false;
                }
                return true;
            }
            else
                return thisNormalized.Equals(objNormalized);
        }

        public override Expression GetDerivation(string parameter)
        {
            if (this.Operands.Count == 0) return new ConstantExpression(0);
            if (this.Operands.Count == 1) return this.Operands.Single().GetDerivation(parameter);
            Expression exp1 = this.Operands.First();
            Expression exp2 = this.Operands.Count == 2 ? this.Operands[1] : new MultiplyExpression(
                this.Operands.Skip(1).ToList());
            var e = new AddExpression(new List<Expression>
            {
                new MultiplyExpression(new List<Expression> { exp1.GetDerivation(parameter),exp2}),
                new MultiplyExpression(new List<Expression> { exp1,exp2.GetDerivation(parameter)}),
            }).Normalize();
            return e;
        }

        public override Expression Normalize()
        {
            if (Normalized)
                return this;
            if (this.Operands.Count == 0) return new ConstantExpression(0);
            if (this.Operands.Count == 1) return this.Operands.Single().Normalize();
            List<Expression> finalResult = new List<Expression>();
            finalResult.Add(new ConstantExpression(1));
            foreach (var operand in Operands)
            {
                List<Expression> currentResult = new List<Expression>();
                var operandNormalized = operand.Normalize();
                foreach (var operand2 in finalResult)
                {
                    ReadOnlyCollection<Expression> operandSubs = operandNormalized is AddExpression ?
                        (operandNormalized as AddExpression).Operands : new List<Expression> { operandNormalized }.AsReadOnly();
                    foreach (var operandSub in operandSubs)
                    {
                        if (operand2 is AddExpression || operandSub is AddExpression)
                            throw new Exception();
                        List<Expression> allFactors = new List<Expression>();
                        foreach (var o in new List<Expression> { operand2, operandSub })
                        {
                            if (o is MultiplyExpression)
                                allFactors.AddRange((o as MultiplyExpression).Operands);
                            else
                                allFactors.Add(o);
                        }
                        List<ParameterExpression> allParameters = allFactors.OfType<ParameterExpression>().ToList();
                        List<ConstantExpression> allConstants = allFactors.OfType<ConstantExpression>().ToList();
                        double constant = 1;
                        foreach (var c in allConstants)
                            constant *= c.Constant;
                        List<Expression> allFactorsNormalized = new List<Expression>();
                        if (constant != 0)
                        {
                            if (constant != 1 || allParameters.Count == 0)
                                allFactorsNormalized.Add(new ConstantExpression(constant));
                            allFactorsNormalized.AddRange(allParameters.OrderBy(_ => _.ParameterName));
                        }
                        if (allFactorsNormalized.Count == 1)
                            currentResult.Add(allFactorsNormalized.Single());
                        if (allFactorsNormalized.Count > 1)
                        {
                            var expr = new MultiplyExpression(allFactorsNormalized);
                            expr.Normalized = true;
                            currentResult.Add(expr);
                        }
                    }
                }
                finalResult = currentResult;
            }
            return new AddExpression(finalResult).Normalize();
        }

        public override Expression ReplaceParameter(string name, Expression expr)
        {
            return new MultiplyExpression(operands.Select(_ => _.ReplaceParameter(name, expr)).ToList());
        }

        public ReadOnlyCollection<Expression> Operands { get { return operands.AsReadOnly(); } }
        private List<Expression> operands;
    }
    


    public class CompareExpression
        : IComparer<Expression>
    {
        public static CompareExpression Instance
        {
            get
            {
                if (instance == null)
                    instance = new CompareExpression();
                return instance;
            }
        }

        private static CompareExpression instance = null;

        private double GetConstant(Expression e)
        {
            if (e is MultiplyExpression)
            {
                if ((e as MultiplyExpression).Operands.First() is ConstantExpression)
                    return ((e as MultiplyExpression).Operands.First() as ConstantExpression).Constant;
                return 1;
            }
            if (e is ParameterExpression)
                return 1;
            if (e is ConstantExpression)
                return (e as ConstantExpression).Constant;
            throw new Exception();
        }

        private List<ParameterExpression> GetParameters(Expression expr)
        {
            if (expr is MultiplyExpression)
            {
                if ((expr as MultiplyExpression).Operands.First() is ConstantExpression)
                {
                    return (expr as MultiplyExpression).Operands.Skip(1).OfType<ParameterExpression>().ToList();
                }
                return (expr as MultiplyExpression).Operands.OfType<ParameterExpression>().ToList();
            }
            if (expr is ParameterExpression)
                return new List<ParameterExpression> { expr as ParameterExpression };
            if (expr is ConstantExpression)
                return new List<ParameterExpression>();
            throw new Exception();
        }

        public int Compare(Expression x, Expression y)
        {
            if (x is AddExpression || y is AddExpression)
                throw new NotImplementedException();
            if (!x.Normalized || !y.Normalized)
                throw new NotImplementedException();
            double constant1 = GetConstant(x);
            double constant2 = GetConstant(y);
            var parm1 = GetParameters(x);
            var parm2 = GetParameters(y);
            int iMax = Math.Min(parm1.Count, parm2.Count);
            for (int i = 0; i < iMax; ++i)
            {
                if (parm1[i].ParameterName != parm2[i].ParameterName)
                    return string.Compare(parm1[i].ParameterName, parm2[i].ParameterName);
            }
            if (parm1.Count != parm2.Count)
                return parm1.Count < parm2.Count ? -1 : 1;
            if (constant1 != constant2)
                return constant1 < constant2 ? -1 : 1;
            return 0;
        }
    }
}
