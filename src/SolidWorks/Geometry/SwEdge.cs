﻿//*********************************************************************
//xCAD
//Copyright(C) 2020 Xarial Pty Limited
//Product URL: https://www.xcad.net
//License: https://xcad.xarial.com/license/
//*********************************************************************

using SolidWorks.Interop.sldworks;
using System.Collections.Generic;
using Xarial.XCad.Geometry;
using Xarial.XCad.Geometry.Structures;
using Xarial.XCad.Geometry.Wires;
using Xarial.XCad.SolidWorks.Geometry.Curves;
using Xarial.XCad.Utils.Reflection;

namespace Xarial.XCad.SolidWorks.Geometry
{
    public class SwEdge : SwEntity, IXEdge
    {
        IXSegment IXEdge.Definition => Definition;

        public IEdge Edge { get; }

        public override SwBody Body => FromDispatch<SwBody>(Edge.GetBody());

        public override IEnumerable<SwEntity> AdjacentEntities 
        {
            get 
            {
                foreach (IFace2 face in (Edge.GetTwoAdjacentFaces2() as object[]).ValueOrEmpty()) 
                {
                    yield return FromDispatch<SwFace>(face);
                }

                foreach (ICoEdge coEdge in (Edge.GetCoEdges() as ICoEdge[]).ValueOrEmpty())
                {
                    var edge = coEdge.GetEdge() as IEdge;
                    yield return FromDispatch<SwEdge>(edge);
                }

                //TODO: implement vertices
            }
        }

        public SwCurve Definition => FromDispatch<SwCurve>(Edge.IGetCurve());

        internal SwEdge(IEdge edge) : base(edge as IEntity)
        {
            Edge = edge;
        }
    }

    public interface ISwCircularEdge : IXCircularEdge 
    {
        new ISwArcCurve Definition { get; }
    }

    public class SwCircularEdge : SwEdge, ISwCircularEdge
    {
        IXArc IXCircularEdge.Definition => Definition;

        internal SwCircularEdge(IEdge edge) : base(edge)
        {
        }

        public new ISwArcCurve Definition => SwSelObject.FromDispatch<SwArcCurve>(this.Edge.IGetCurve());
    }

    public interface ISwLinearEdge : IXLinearEdge 
    {
        new ISwLineCurve Definition { get; }
    }

    public class SwLinearEdge : SwEdge, ISwLinearEdge
    {
        IXLine IXLinearEdge.Definition => Definition;

        internal SwLinearEdge(IEdge edge) : base(edge)
        {
        }

        public new ISwLineCurve Definition => SwSelObject.FromDispatch<SwLineCurve>(this.Edge.IGetCurve());
    }
}