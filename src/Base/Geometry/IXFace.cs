﻿//*********************************************************************
//xCAD
//Copyright(C) 2022 Xarial Pty Limited
//Product URL: https://www.xcad.net
//License: https://xcad.xarial.com/license/
//*********************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using Xarial.XCad.Features;
using Xarial.XCad.Geometry.Structures;
using Xarial.XCad.Geometry.Surfaces;

namespace Xarial.XCad.Geometry
{
    /// <summary>
    /// Represents face entity
    /// </summary>
    public interface IXFace : IXEntity, IXColorizable
    {
        /// <summary>
        /// True if the direction of the face conicides with the direction of its surface definition, False if the directions are opposite
        /// </summary>
        bool Sense { get; }

        /// <summary>
        /// Area of the face
        /// </summary>
        double Area { get; }

        /// <summary>
        /// Underlying definition for this face
        /// </summary>
        IXSurface Definition { get; }

        /// <summary>
        /// Returns the feature which owns this face
        /// </summary>
        IXFeature Feature { get; }

        /// <summary>
        /// Edges of this face
        /// </summary>
        IEnumerable<IXEdge> Edges { get; }

        /// <summary>
        /// Projects the specified point onto the surface
        /// </summary>
        /// <param name="point">Input point</param>
        /// <param name="direction">Projection direction</param>
        /// <param name="projectedPoint">Projected point or null</param>
        /// <returns>True if projected point is found, false - if not</returns>
        bool TryProjectPoint(Point point, Vector direction, out Point projectedPoint);

        /// <summary>
        /// Finds the boundary of this face
        /// </summary>
        /// <param name="uMin">Minimum u-parameter</param>
        /// <param name="uMax">Maximum u-parameter</param>
        /// <param name="vMin">Minimum v-parameter</param>
        /// <param name="vMax">Maximum v-parameter</param>
        void GetUVBoundary(out double uMin, out double uMax, out double vMin, out double vMax);

        /// <summary>
        /// Finds u and v parameters of the face based on the point location
        /// </summary>
        /// <param name="point">Point</param>
        /// <param name="uParam">U-parameter</param>
        /// <param name="vParam">V-parameter</param>
        void CalculateUVParameter(Point point, out double uParam, out double vParam);
    }

    /// <summary>
    /// Represents planar face
    /// </summary>
    public interface IXPlanarFace : IXFace, IXRegion
    {
        /// <inheritdoc/>
        new IXPlanarSurface Definition { get; }
    }

    /// <summary>
    /// Represents cylindrical face
    /// </summary>
    public interface IXCylindricalFace : IXFace 
    {
        /// <inheritdoc/>
        new IXCylindricalSurface Definition { get; }
    }

    public interface IXBlendXFace : IXFace 
    {
    }

    public interface IXBFace : IXFace
    {
    }

    public interface IXConicalFace : IXFace
    {
    }

    public interface IXExtrudedFace : IXFace
    {
    }

    public interface IXOffsetFace : IXFace
    {
    }

    public interface IXRevolvedFace : IXFace
    {
    }

    public interface IXSphericalFace : IXFace
    {
    }

    public interface IXToroidalFace : IXFace
    {
    }
}
