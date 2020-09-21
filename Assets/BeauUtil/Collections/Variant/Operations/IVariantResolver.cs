/*
 * Copyright (C) 2017-2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    16 Sept 2020
 * 
 * File:    IVariantResolver.cs
 * Purpose: Resolution for variants.
 */

using System;

namespace BeauUtil.Variants
{
    public interface IVariantResolver
    {
        /// <summary>
        /// Performs any remapping necessary for the given key.
        /// </summary>
        void RemapKey(ref TableKeyPair ioKey);

        /// <summary>
        /// Attempts to get a variant table from the given table id.
        /// </summary>
        bool TryGetTable(object inContext, StringHash inTableId, out VariantTable outTable);
        
        /// <summary>
        /// Attempts to get a variant value from the given variant key.
        /// If this returns false, retrieval of variables
        /// will occur through TryGetTable and VariantTable's methods.
        /// </summary>
        bool TryGetVariant(object inContext, TableKeyPair inKey, out Variant outVariant);
    }

    static public class VariantResolverExtensions
    {
        /// <summary>
        /// Attempts to resolve a variable.
        /// </summary>
        static public bool TryResolve(this IVariantResolver inResolver, object inContext, TableKeyPair inKey, out Variant outVariant)
        {
            inResolver.RemapKey(ref inKey);

            bool bRetrieved = inResolver.TryGetVariant(inContext, inKey, out outVariant);
            if (bRetrieved)
                return true;

            VariantTable table;
            bool bFoundTable = inResolver.TryGetTable(inContext, inKey.TableId, out table);
            if (!bFoundTable || table == null)
            {
                UnityEngine.Debug.LogErrorFormat("[IVariantResolver] Unable to retrieve table with id '{0}'", inKey.TableId.ToDebugString());
                outVariant = Variant.Null;
                return false;
            }

            return table.TryLookup(inKey.VariableId, out outVariant);
        }

        /// <summary>
        /// Attempts to apply a modification.
        /// </summary>
        static public bool TryModify(this IVariantResolver inResolver, object inContext, TableKeyPair inKey, VariantModifyOperator inOperator, Variant inVariant)
        {
            inResolver.RemapKey(ref inKey);

            VariantTable table;
            bool bRetrieved = inResolver.TryGetTable(inContext, inKey.TableId, out table);
            if (!bRetrieved || table == null)
            {
                UnityEngine.Debug.LogErrorFormat("[IVariantResolver] Unable to retrieve table with id '{0}'", inKey.TableId.ToDebugString());
                return false;
            }

            table.Modify(inKey.VariableId, inOperator, inVariant);
            return true;
        }

        /// <summary>
        /// Attempts to apply one or more modifications, described by the given string.
        /// </summary>
        static public bool TryModify(this IVariantResolver inResolver, object inContext, StringSlice inModifyData)
        {
            if (inModifyData.IsWhitespace)
                return true;
            
            if (inModifyData.Contains(','))
            {
                StringSlice.ISplitter splitter = QuoteAwareSplitter ?? (QuoteAwareSplitter = new StringUtils.ArgsList.Splitter(false));
                bool bSuccess = true;
                VariantModification mod;
                foreach(var group in inModifyData.EnumeratedSplit(splitter, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!VariantModification.TryParse(group, out mod) || !mod.Execute(inResolver, inContext))
                        bSuccess = false;
                }

                return bSuccess;
            }
            else
            {
                VariantModification mod;
                return VariantModification.TryParse(inModifyData, out mod) && mod.Execute(inResolver, inContext);
            }
        }

        /// <summary>
        /// Attempts to evaluate if all conditions described by the given string are true.
        /// </summary>
        static public bool TryEvaluate(this IVariantResolver inResolver, object inContext, StringSlice inEvalData)
        {
            if (inEvalData.IsWhitespace)
                return true;
            
            if (inEvalData.Contains(','))
            {
                StringSlice.ISplitter splitter = QuoteAwareSplitter ?? (QuoteAwareSplitter = new StringUtils.ArgsList.Splitter(false));
                VariantComparison comp;
                foreach(var group in inEvalData.EnumeratedSplit(splitter, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!VariantComparison.TryParse(group, out comp) || !comp.Evaluate(inResolver, inContext))
                        return false;
                }

                return true;
            }
            else
            {
                VariantComparison comp;
                return VariantComparison.TryParse(inEvalData, out comp) && comp.Evaluate(inResolver, inContext);
            }
        }

        [ThreadStatic] static private StringUtils.ArgsList.Splitter QuoteAwareSplitter;
    }
}