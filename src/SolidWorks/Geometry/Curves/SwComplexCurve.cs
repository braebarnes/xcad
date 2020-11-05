﻿using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xarial.XCad.Geometry.Curves;

namespace Xarial.XCad.SolidWorks.Geometry.Curves
{
    public interface ISwComplexCurve : ISwCurve, IXComplexCurve 
    {
        new ISwCurve[] Composition { get; set; }
    }

    internal class SwComplexCurve : SwCurve, ISwComplexCurve
    {
        IXCurve[] IXComplexCurve.Composition 
        {
            get => Composition;
            set => Composition = value?.Cast<SwCurve>().ToArray();
        }

        public ISwCurve[] Composition { get; set; }

        internal SwComplexCurve(IModeler modeler, ICurve[] curves, bool isCreated) 
            : base(modeler, curves, isCreated)
        {
        }

        protected override ICurve[] Create()
        {
            var retVal = new List<ICurve>();

            foreach(var comp in Composition) 
            {
                if (!comp.IsCommitted)
                {
                    comp.Commit();
                }

                retVal.AddRange(comp.Curves);
            }

            return retVal.ToArray();
        }
    }
}
