﻿//*********************************************************************
//xCAD
//Copyright(C) 2022 Xarial Pty Limited
//Product URL: https://www.xcad.net
//License: https://xcad.xarial.com/license/
//*********************************************************************

using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Xarial.XCad.Features;
using Xarial.XCad.Sketch;
using Xarial.XCad.SolidWorks.Documents;
using Xarial.XCad.SolidWorks.Sketch;

namespace Xarial.XCad.SolidWorks.Features
{
    public interface ISwSketchBase : IXSketchBase, ISwFeature
    {
        //TODO: think how to remove the below functions
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        bool GetEditMode(ISketch sketch);
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        void SetEditMode(ISketch sketch, bool isEditing);

        ISketch Sketch { get; }
    }

    internal abstract class SwSketchBase : SwFeature, ISwSketchBase
    {
        private readonly SwSketchEntityCollection m_SwEntsColl;

        public ISketch Sketch => m_Sketch;

        public override object Dispatch => Sketch;

        private ISketch m_Sketch;

        internal SwSketchBase(IFeature feat, ISwDocument doc, ISwApplication app, bool created) 
            : this(feat, (ISketch)feat?.GetSpecificFeature2(), doc, app, created)
        {
        }

        internal SwSketchBase(ISketch sketch, ISwDocument doc, ISwApplication app, bool created) : this((IFeature)sketch, sketch, doc, app, created)
        {
        }

        private SwSketchBase(IFeature feat, ISketch sketch, ISwDocument doc, ISwApplication app, bool created) : base(feat, doc, app, created)
        {
            if (doc == null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            m_SwEntsColl = new SwSketchEntityCollection(this, doc, app);
            m_Sketch = sketch;
        }

        public IXSketchEntityRepository Entities => m_SwEntsColl;

        public bool IsEditing
        {
            get
            {
                if (IsCommitted)
                {
                    return GetEditMode(Sketch);
                }
                else
                {
                    throw new Exception("This option is only valid for the committed sketch");
                }
            }
            set
            {
                if (IsCommitted)
                {
                    SetEditMode(Sketch, value);
                }
                else
                {
                    throw new Exception("This option is only valid for the committed sketch");
                }
            }
        }

        public bool GetEditMode(ISketch sketch)
            => OwnerModelDoc.SketchManager.ActiveSketch == sketch;

        public void SetEditMode(ISketch sketch, bool isEditing)
        {
            if (isEditing)
            {
                if (!GetEditMode(sketch))
                {
                    //TODO: use API only selection
                    (sketch as IFeature).Select2(false, 0);
                    ToggleEditSketch();
                }
            }
            else
            {
                if (GetEditMode(sketch))
                {
                    ToggleEditSketch();
                }
            }
        }

        protected abstract void ToggleEditSketch();

        protected override IFeature CreateFeature(CancellationToken cancellationToken)
        {
            var sketch = CreateSketch();

            m_SwEntsColl.CommitCache(sketch, cancellationToken);

            m_Sketch = sketch;

            return (IFeature)sketch;
        }

        protected abstract ISketch CreateSketch();
    }
}