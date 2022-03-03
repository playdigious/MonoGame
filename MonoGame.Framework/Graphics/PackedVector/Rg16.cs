// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector
{
    /// <summary>
    /// Packed vector type containing a single 8 bit normalized W values that is ranging from 0 to 1.
    /// </summary>
    public struct Rg16 : IPackedVector<UInt16>, IEquatable<Rg16>, IPackedVector
    {
        private UInt16 packedValue;

        private static UInt16 Pack(float x, float y)
        {
            var byte2 = (((ushort)Math.Round(MathHelper.Clamp(x, -1.0f, 1.0f) * 127.0f)) & 0xFF) << 0;
            var byte1 = (((ushort)Math.Round(MathHelper.Clamp(y, -1.0f, 1.0f) * 127.0f)) & 0xFF) << 8;

            return (UInt16)(byte2 | byte1);
        }

        /// <summary>
        /// Gets and sets the packed value.
        /// </summary>
        [CLSCompliant(false)]
        public UInt16 PackedValue
        {
            get
            {
                return packedValue;
            }
            set
            {
                packedValue = value;
            }
        }

        /// <summary>
        /// Creates a new instance of Rg16.
        /// </summary>
        /// <param name="rg">The rg component</param>
        public Rg16(float r, float g)
        {
            packedValue = Pack(r, g);
        }

        /// <summary>
        /// Creates a new instance of Rg16.
        /// </summary>
        /// <param name="vector">The vector2 component</param>
        public Rg16(Vector2 vector)
        {
            packedValue = Pack(vector.X, vector.Y);
        }

        /// <summary>
        /// Gets the packed vector in float format.
        /// </summary>
        /// <returns>The packed vector in Vector3 format</returns>
        public float ToAlpha()
        {
            return (float) (packedValue / 255.0f);
        }

        /// <summary>
        /// Sets the packed vector from a Vector4.
        /// </summary>
        /// <param name="vector">Vector containing the components.</param>
        void IPackedVector.PackFromVector4(Vector4 vector)
        {
            packedValue = Pack(vector.X, vector.Y);
        }

        /// <summary>
        /// Gets the packed vector in Vector2 format.
        /// </summary>
        /// <returns>The packed vector in Vector2 format</returns>
        public Vector2 ToVector2()
        {
            return new Vector2(
                ((sbyte)((packedValue >> 0) & 0xFF)) / 127.0f,
                ((sbyte)((packedValue >> 8) & 0xFF)) / 127.0f);
        }

        /// <summary>
        /// Gets the packed vector in Vector4 format.
        /// </summary>
        /// <returns>The packed vector in Vector4 format</returns>
        public Vector4 ToVector4()
        {
            return new Vector4(ToVector2(), 0.0f, 1.0f);
        }

        /// <summary>
        /// Compares an object with the packed vector.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the object is equal to the packed vector.</returns>
        public override bool Equals(object obj)
        {
            return (obj is Alpha8) && Equals((Alpha8) obj);
        }

        /// <summary>
        /// Compares another Alpha8 packed vector with the packed vector.
        /// </summary>
        /// <param name="other">The Alpha8 packed vector to compare.</param>
        /// <returns>True if the packed vectors are equal.</returns>
        public bool Equals(Rg16 other)
        {
            return packedValue == other.packedValue;
        }

        /// <summary>
        /// Gets a string representation of the packed vector.
        /// </summary>
        /// <returns>A string representation of the packed vector.</returns>
        public override string ToString()
        {
            return (packedValue / 255.0f).ToString();
        }

        /// <summary>
        /// Gets a hash code of the packed vector.
        /// </summary>
        /// <returns>The hash code for the packed vector.</returns>
        public override int GetHashCode()
        {
            return packedValue.GetHashCode();
        }

        public static bool operator ==(Rg16 lhs, Rg16 rhs)
        {
            return lhs.packedValue == rhs.packedValue;
        }

        public static bool operator !=(Rg16 lhs, Rg16 rhs)
        {
            return lhs.packedValue != rhs.packedValue;
        }

        private static byte Pack(float alpha)
        {
            return (byte) Math.Round(
                MathHelper.Clamp(alpha, 0, 1) * 255.0f
            );
        }
    }
}
