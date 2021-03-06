﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HandlebarsDotNet.PathStructure;

namespace HandlebarsDotNet.Compiler
{
    internal abstract class BlockAccumulatorContext
    {
        private static readonly HashSet<string> ConditionHelpers = new HashSet<string>(StringComparer.OrdinalIgnoreCase){ "#if", "#unless", "^if", "^unless" };
        private static readonly HashSet<string> IteratorHelpers = new HashSet<string>(StringComparer.OrdinalIgnoreCase){ "#each", "^each" };

        public static BlockAccumulatorContext Create(Expression item, ICompiledHandlebarsConfiguration configuration)
        {
            BlockAccumulatorContext context = null;
            if (IsConditionalBlock(item))
            {
                context = new ConditionalBlockAccumulatorContext(item);
            }
            else if (IsPartialBlock(item))
            {
                context = new PartialBlockAccumulatorContext(item);
            }
            else if (IsIteratorBlock(item))
            {
                context = new IteratorBlockAccumulatorContext(item);
            }
            else if (IsBlockHelper(item, configuration))
            {
                context = new BlockHelperAccumulatorContext(item);
            }

            return context;
        }

        public abstract string BlockName { get; protected set; }

        private static bool IsConditionalBlock(Expression item)
        {
            item = UnwrapStatement(item);
            return item is HelperExpression helperExpression 
                   && ConditionHelpers.Contains(helperExpression.HelperName);
        }

        private static bool IsBlockHelper(Expression item, ICompiledHandlebarsConfiguration configuration)
        {
            item = UnwrapStatement(item);
            if (item is HelperExpression hitem)
            {
                var helperName = hitem.HelperName;
                var helperPathInfo = PathInfo.Parse(helperName);
                return hitem.IsBlock || !configuration.Helpers.ContainsKey(helperPathInfo) && configuration.BlockHelpers.ContainsKey(helperPathInfo);
            }
            return false;
        }

        private static bool IsIteratorBlock(Expression item)
        {
            item = UnwrapStatement(item);
            return item is HelperExpression expression 
                   && IteratorHelpers.Contains(expression.HelperName);
        }

        private static bool IsPartialBlock (Expression item)
        {
            item = UnwrapStatement (item);
            switch (item)
            {
                case PathExpression expression:
                    return expression.Path.StartsWith("#>");
                
                case HelperExpression helperExpression:
                    return helperExpression.HelperName.StartsWith("#>");
                
                default:
                    return false;
            }
        }

        protected static Expression UnwrapStatement(Expression item)
        {
            if (item is StatementExpression expression)
            {
                return expression.Body;
            }

            return item;
        }

        protected BlockAccumulatorContext(Expression startingNode)
        {
        }

        public abstract void HandleElement(Expression item);

        public abstract bool IsClosingElement(Expression item);

        public abstract Expression GetAccumulatedBlock();
    }
}

