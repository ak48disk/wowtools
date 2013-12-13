﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ExpressionCalc
{
    class Parser
    {
        public Parser(Lexer l)
        {
            this.l = l;
            accept();
        }

        private Expression factor()
        {
            Expression retVal;
            Lexer.Token id;
            switch (lookAhead.TokenType)
            {
                case '(':
                    accept(); retVal = expr(); match(')');
                    break;
                case Lexer.TOKEN_ID:
                    id = match(Lexer.TOKEN_ID);
                    return new ParameterExpression(id.strToken);
                case Lexer.TOKEN_NUMBER:
                    id = match(Lexer.TOKEN_NUMBER);
                    return new ConstantExpression(id.numToken);
                case '-':
                    accept();
                    id = match(Lexer.TOKEN_NUMBER);
                    return new ConstantExpression(-id.numToken);
                default:
                    throw new Exception();
            }
            return retVal;
        }

        private Expression expr1(int priority)
        {
            Expression retVal;
            Expression right;

            if (priority >= 0)
                retVal = expr1(priority - 1);
            else
                return factor();

            while (inPriority(lookAhead.TokenType, priority))
            {
                switch (lookAhead.TokenType)
                {
                    case '+':
                        accept();
                        right = expr1(priority - 1);
                        retVal = new AddExpression(new List<Expression>{
                            retVal,right
                        });
                        break;
                    case '-':
                        accept();
                        right = expr1(priority - 1);
                        retVal = new AddExpression(new List<Expression>{
                            retVal,new MultiplyExpression (
                                new List<Expression> {
                                    new ConstantExpression(-1),
                                    right
                                }
                            )
                        });
                        break;
                    case '*':
                        accept();
                        right = expr1(priority - 1);
                        retVal = new MultiplyExpression(new List<Expression>{
                            retVal,right
                        });
                        break;
                    default:
                        return retVal;
                }
            }
            return retVal;
        }

        private Expression expr()
        {
            return expr1(1);
        }

        public static Expression ParseExpression(string str)
        {
            Parser p = new Parser(new Lexer(str));
            return p.expr().Normalize();
        }

        public static Context ParseFile(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                Lexer l = new Lexer(fs);
                Parser p = new Parser(l);
                Context ctx = new Context();
                p.parseFile(ctx);
                return ctx;
            }
        }

        private void parseFile(Context ctx)
        {
            while (!l.eof())
            {
                Lexer.Token token = match(Lexer.TOKEN_ID);
                match('=');
                Expression e = expr();
                match(';');
                ctx.AddExpression(token.strToken, e);
            }
        }

        private void accept()
        {
            lookAhead = l.next();
        }

        private Lexer.Token match(int Token_type)
        {
            Lexer.Token retVal = lookAhead;
            if (lookAhead.TokenType == Token_type)
                accept();
            else
                throw new Exception();
            return retVal;
        }

        private bool inPriority(int TokenType, int priority)
        {
            int i = 0;
            while (priorities[priority, i] != -1)
            {
                if (priorities[priority, i] == TokenType) return true;
                ++i;
            }
            return false;
        }

        private static int[,] priorities = 
        {
	        {'*', -1},
	        {'+', -1},
        };

        private Lexer.Token lookAhead;
        Lexer l;
    }
}
