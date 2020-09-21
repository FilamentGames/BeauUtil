/*
 * Copyright (C) 2017-2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    7 Sept 2020
 * 
 * File:    VariantTable.cs
 * Purpose: Collection of named variant values.
 */

#if CSHARP_7_3_OR_NEWER
#define EXPANDED_REFS
#endif // CSHARP_7_3_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BeauUtil.Variants
{
    /// <summary>
    /// Collection of named variant values.
    /// </summary>
    public class VariantTable : IEnumerable<NamedVariant>, IReadOnlyList<NamedVariant>
    {
        private StringHash m_Name;
        private RingBuffer<NamedVariant> m_Values;
        private VariantTable m_Base;
        private bool m_Optimized = true;

        public VariantTable()
        {
            m_Values = new RingBuffer<NamedVariant>();
        }

        public VariantTable(StringHash inName)
            : this()
        {
            m_Name = inName;
        }

        public VariantTable(StringHash inName, VariantTable inBase)
            : this(inName)
        {
            m_Base = inBase;
        }

        /// <summary>
        /// Name of the variant table.
        /// </summary>
        public StringHash Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        /// <summary>
        /// Number of values in this table.
        /// </summary>
        public int Count
        {
            get { return m_Values.Count; }
        }

        /// <summary>
        /// Current value capacity.
        /// </summary>
        public int Capacity
        {
            get { return m_Values.Capacity; }
            set { m_Values.SetCapacity(value); }
        }

        /// <summary>
        /// Retrieves the value at the given index.
        /// </summary>
        public NamedVariant this[int index]
        {
            get { return m_Values[index]; }
        }

        /// <summary>
        /// Sets the value for the given id.
        /// </summary>
        public void Set(StringHash inId, Variant inValue)
        {
            SetAt(IndexOf(inId), inId, inValue);
        }

        /// <summary>
        /// Retrieves the value on this table.
        /// Will not look into base tables.
        /// </summary>
        public Variant Get(StringHash inId)
        {
            return GetAt(IndexOf(inId));
        }

        /// <summary>
        /// Gets/sets the variants for the given id.
        /// </summary>
        public Variant this[StringHash inId]
        {
            get { return Get(inId); }
            set { Set(inId, value); }
        }

        /// <summary>
        /// Returns if a value with the given id exists on this table.
        /// </summary>
        public bool Has(StringHash inId)
        {
            return IndexOf(inId) >= 0;
        }

        /// <summary>
        /// Attempts to delete the value with the given id.
        /// </summary>
        public bool Delete(StringHash inId)
        {
            int idx = IndexOf(inId);
            if (idx >= 0)
            {
                m_Values.FastRemoveAt(idx);
                m_Optimized = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears all values from this table.
        /// </summary>
        public void Clear()
        {
            m_Values.Clear();
            m_Optimized = true;
        }

        /// <summary>
        /// The base VariantTable.
        /// Values that do not exist in this VariantTable will be looked up in the base.
        /// </summary>
        public VariantTable Base
        {
            get { return m_Base; }
            set
            {
                if (m_Base != value)
                {
                    if (value != null)
                    {
                        if (value == this || value.m_Base == this)
                            throw new InvalidOperationException("Provided parent would cause infinite loop");
                    }

                    m_Base = value;
                }
            }
        }

        /// <summary>
        /// Optimizes lookups.
        /// </summary>
        public void Optimize()
        {
            if (m_Optimized)
                return;

            if (m_Values.Count > 3)
            {
                m_Values.Sort(EntryComparer.Instance);
            }
            m_Optimized = true;
        }

        /// <summary>
        /// Attempts to retrieve a value from the table.
        /// If not present in this table, it will look in the parent.
        /// </summary>
        public bool TryLookup(StringHash inId, out Variant outValue)
        {
            Optimize();

            int idx = IndexOf(inId);
            if (idx >= 0)
            {
                outValue = m_Values[idx].Value;
                return true;
            }

            if (m_Base != null)
            {
                return m_Base.TryLookup(inId, out outValue);
            }

            outValue = Variant.Null;
            return false;
        }

        /// <summary>
        /// Modify a value on the table.
        /// </summary>
        public void Modify(StringHash inId, VariantModifyOperator inOperator, Variant inOperand)
        {
            int idx = IndexOf(inId);
            switch(inOperator)
            {
                case VariantModifyOperator.Set:
                    {
                        SetAt(idx, inId, inOperand);
                        break;
                    }

                case VariantModifyOperator.Add:
                    {
                        Variant current = GetAt(idx);
                        SetAt(idx, inId, current + inOperand);
                        break;
                    }

                case VariantModifyOperator.Subtract:
                    {
                        Variant current = GetAt(idx);
                        SetAt(idx, inId, current - inOperand);
                        break;
                    }

                case VariantModifyOperator.Multiply:
                    {
                        Variant current = GetAt(idx);
                        SetAt(idx, inId, current * inOperand);
                        break;
                    }

                case VariantModifyOperator.Divide:
                    {
                        Variant current = GetAt(idx);
                        SetAt(idx, inId, current / inOperand);
                        break;
                    }
            }
        }

        private int IndexOf(StringHash inId)
        {
            if (m_Optimized && m_Values.Count > 3)
                return m_Values.BinarySearch(inId, SearchPredicate);
            
            for(int i = 0; i < m_Values.Count; ++i)
            {
                if (m_Values[i].Id == inId)
                    return i;
            }

            return -1;
        }

        private Variant GetAt(int inIdx)
        {
            return inIdx >= 0 ? m_Values[inIdx].Value : Variant.Null;
        }

        private void SetAt(int inIdx, StringHash inId, Variant inValue)
        {
            if (inIdx >= 0)
            {
                #if EXPANDED_REFS
                m_Values[inIdx].Value = inValue;
                #else
                NamedVariant val = m_Values[inIdx];
                val.Value = inValue;
                m_Values[inIdx] = val;
                #endif // EXPANDED_VALUES
                return;
            }

            m_Values.PushBack(new NamedVariant(inId, inValue));
            m_Optimized = false;
        }

        #region Output

        public string ToDebugString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Variant Table '").Append(m_Name.ToDebugString()).Append("', ").Append(Count).Append(" entries");
            for(int i = 0; i < m_Values.Count; ++i)
            {
                sb.Append("\n  ").Append(m_Values[i].ToDebugString());
            }
            return sb.ToString();
        }

        #endregion // Output
        
        #region IEnumerable

        public IEnumerator<NamedVariant> GetEnumerator()
        {
            return m_Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Values.GetEnumerator();
        }

        #endregion // IEnumerable

        #region Binary Search

        private sealed class EntryComparer : IComparer<NamedVariant>
        {
            static internal readonly EntryComparer Instance = new EntryComparer();

            public int Compare(NamedVariant x, NamedVariant y)
            {
                return x.Id.CompareTo(y.Id);
            }
        }

        static private readonly ComparePredicate<NamedVariant, StringHash> SearchPredicate = (nv, sh) => {
            if (sh.HashValue < nv.Id.HashValue)
                return 1;
            else if (sh.HashValue > nv.Id.HashValue)
                return -1;
            return 0;
        };

        #endregion // Binary Search
    }
}