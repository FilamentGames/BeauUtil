/*
 * Copyright (C) 2017-2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    24 August 2020
 * 
 * File:    AbstractBlockGenerator.cs
 * Purpose: Rules for parsing a set of tagged blocks.
 */

using BeauUtil.Tags;

namespace BeauUtil.Blocks
{
    public abstract class AbstractBlockGenerator<TBlock, TPackage> : IBlockGenerator<TBlock, TPackage>
        where TBlock : class, IDataBlock
        where TPackage : class, IDataBlockPackage<TBlock>
    {
        #region Shared Object

        public abstract TPackage CreatePackage(string inFileName);

        public virtual bool TryEvaluatePackage(IBlockParserUtil inUtil, TPackage inPackage, TBlock inCurrentBlock, TagData inMetadata)
        {
            return false;
        }

        #endregion // Shared Object

        #region Parse Stages

        public virtual void OnStart(IBlockParserUtil inUtil, TPackage inPackage) { }

        public virtual void OnBlocksStart(IBlockParserUtil inUtil, TPackage inPackage) { }

        public virtual void OnEnd(IBlockParserUtil inUtil, TPackage inPackage, bool inbError) { }

        #endregion // Parse Stages

        #region Block Actions
        
        public abstract bool TryCreateBlock(IBlockParserUtil inUtil, TPackage inPackage, TagData inId, out TBlock outBlock);

        public virtual bool TryEvaluateMeta(IBlockParserUtil inUtil, TPackage inPackage, TBlock inBlock, TagData inMetadata)
        {
            return false;
        }

        public virtual void CompleteHeader(IBlockParserUtil inUtil, TPackage inPackage, TBlock inBlock, TagData inAdditionalData) { }

        public abstract bool TryAddContent(IBlockParserUtil inUtil, TPackage inPackage, TBlock inBlock, StringSlice inContent);
        
        public virtual void CompleteBlock(IBlockParserUtil inUtil, TPackage inPackage, TBlock inBlock, TagData inAdditionalData, bool inbError) { }

        #endregion // Block Actions

        public virtual bool TryAddComment(IBlockParserUtil inUtil, TPackage inPackage, TBlock inCurrentBlock, StringSlice inComment)
        {
            // Ignore the comment
            return true;
        }
    }
}