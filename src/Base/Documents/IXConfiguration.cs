﻿//*********************************************************************
//xCAD
//Copyright(C) 2020 Xarial Pty Limited
//Product URL: https://www.xcad.net
//License: https://xcad.xarial.com/license/
//*********************************************************************

using Xarial.XCad.Data;

namespace Xarial.XCad.Documents
{
    public interface IXConfiguration : IXObject
    {
        string Name { get; }

        IXPropertyRepository Properties { get; }
    }
}