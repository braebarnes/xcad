﻿//*********************************************************************
//xCAD
//Copyright(C) 2023 Xarial Pty Limited
//Product URL: https://www.xcad.net
//License: https://xcad.xarial.com/license/
//*********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xarial.XCad.Base;
using Xarial.XCad.Documents.Delegates;
using Xarial.XCad.Documents.Enums;
using Xarial.XCad.Geometry;

namespace Xarial.XCad.Documents
{
    /// <summary>
    /// Represents the collection of <see cref="IXComponent"/>
    /// </summary>
    public interface IXComponentRepository : IXRepository<IXComponent>
    {
        /// <summary>
        /// Returns the total count of components including all nested components
        /// </summary>
        int TotalCount { get; }
    }

    /// <summary>
    /// Additonal methods of <see cref="IXComponentRepository"/>
    /// </summary>
    public static class XComponentRepositoryExtension 
    {
        /// <summary>
        /// Returns all components, including children
        /// </summary>
        /// <param name="repo">Components repository</param>
        /// <returns>All components</returns>
        public static IEnumerable<IXComponent> Flatten(this IXComponentRepository repo) 
        {
            foreach (var comp in repo) 
            {
                yield return comp;

                IXComponentRepository children = null;

                var state = comp.State;

                if (!comp.State.HasFlag(ComponentState_e.Suppressed) && !comp.State.HasFlag(ComponentState_e.SuppressedIdMismatch))
                {
                    children = comp.Children;
                }
                else
                {
                    children = null;
                }

                if (children != null)
                {
                    foreach (var subComp in Flatten(children))
                    {
                        yield return subComp;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a template for part component
        /// </summary>
        /// <returns>Part component template</returns>
        public static IXPartComponent PreCreatePartComponent(this IXComponentRepository repo) => repo.PreCreate<IXPartComponent>();

        /// <summary>
        /// Creates a template for assembly component
        /// </summary>
        /// <returns>Assembly component template</returns>
        public static IXAssemblyComponent PreCreateAssemblyComponent(this IXComponentRepository repo) => repo.PreCreate<IXAssemblyComponent>();
    }
}
