using KtxSharp;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xna.Framework.Content.Pipeline.Graphics
{
    public abstract class AstcBitmapContent : BitmapContent
    {
        internal byte[] _bitmapData;

        public AstcBitmapContent()
            : base()
        {
        }

        public AstcBitmapContent(int width, int height)
            : base(width, height)
        {
        }

        public override byte[] GetPixelData()
        {
            if (_bitmapData == null)
                throw new InvalidOperationException("No data set on bitmap");
            var result = new byte[_bitmapData.Length];
            Buffer.BlockCopy(_bitmapData, 0, result, 0, _bitmapData.Length);
            return result;
        }

        public override void SetPixelData(byte[] sourceData)
        {
            _bitmapData = sourceData;
        }

        protected override bool TryCopyFrom(BitmapContent sourceBitmap, Rectangle sourceRegion, Rectangle destinationRegion)
        {
            return false;
        }

        protected override bool TryCopyTo(BitmapContent destinationBitmap, Rectangle sourceRegion, Rectangle destinationRegion)
        {
            return false;
        }
    }
}
