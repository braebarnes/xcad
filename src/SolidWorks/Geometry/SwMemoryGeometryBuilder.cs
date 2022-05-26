﻿//*********************************************************************
//xCAD
//Copyright(C) 2022 Xarial Pty Limited
//Product URL: https://www.xcad.net
//License: https://xcad.xarial.com/license/
//*********************************************************************

using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xarial.XCad.Geometry;
using Xarial.XCad.Geometry.Structures;
using Xarial.XCad.Geometry.Surfaces;
using Xarial.XCad.Geometry.Wires;
using Xarial.XCad.SolidWorks.Geometry.Surfaces;
using Xarial.XCad.SolidWorks.Services;
using Xarial.XCad.Toolkit.Data;
using Xarial.XCad.Toolkit;

namespace Xarial.XCad.SolidWorks.Geometry
{
    public interface ISwMemoryGeometryBuilder : IXMemoryGeometryBuilder
    {
    }

    internal class SwMemoryGeometryBuilder : ISwMemoryGeometryBuilder
    {
        public IXWireGeometryBuilder WireBuilder { get; }
        public IXSheetGeometryBuilder SheetBuilder { get; }
        public IXSolidGeometryBuilder SolidBuilder { get; }

        private readonly SwApplication m_App;

        private readonly IModeler m_Modeler;

        private readonly IMemoryGeometryBuilderToleranceProvider m_TolProvider;

        internal SwMemoryGeometryBuilder(SwApplication app, IMemoryGeometryBuilderDocumentProvider geomBuilderDocsProvider, IMemoryGeometryBuilderToleranceProvider tolProvider) 
        {
            m_App = app;
            m_TolProvider = tolProvider;

            m_Modeler = app.Sw.IGetModeler();

            WireBuilder = new SwMemoryWireGeometryBuilder(app);
            SheetBuilder = new SwMemorySheetGeometryBuilder(app, m_TolProvider);
            SolidBuilder = new SwMemorySolidGeometryBuilder(app, geomBuilderDocsProvider, m_TolProvider);
        }

        public IXBody DeserializeBody(Stream stream)
        {
            var comStr = new StreamWrapper(stream);
            var body = (IBody2)m_Modeler.Restore(comStr);
            return m_App.CreateObjectFromDispatch<ISwTempBody>(body, null);
        }

        public void SerializeBody(IXBody body, Stream stream)
        {
            var comStr = new StreamWrapper(stream);
            ((SwBody)body).Body.Save(comStr);
        }

        public IXPlanarRegion CreateRegionFromSegments(params IXSegment[] segments) => new SwPlanarRegion(segments, this);
    }
}
